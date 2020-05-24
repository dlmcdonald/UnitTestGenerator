using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace UnitTestGenerator.Mac.Adornments
{
    abstract class IntraTextAdornmentTagger<TData, TAdornment>
        : ITagger<XPlatIntraTextAdornmentTag>
        where TAdornment : NSView
    {
        protected readonly ICocoaTextView _view;
        Dictionary<SnapshotSpan, TAdornment> _adornmentCache = new Dictionary<SnapshotSpan, TAdornment>();
        protected ITextSnapshot Snapshot { get; private set; }
        readonly List<SnapshotSpan> _invalidatedSpans = new List<SnapshotSpan>();

        protected IntraTextAdornmentTagger(ICocoaTextView view)
        {
            _view = view;
            Snapshot = view.TextBuffer.CurrentSnapshot;

            _view.LayoutChanged += HandleLayoutChanged;
            _view.TextBuffer.Changed += HandleBufferChanged;
        }

        /// <param name="span">The span of text that this adornment will elide.</param>
        /// <returns>Adornment corresponding to given data. May be null.</returns>
        protected abstract TAdornment CreateAdornment(TData data, SnapshotSpan span);

        /// <returns>True if the adornment was updated and should be kept. False to have the adornment removed from the view.</returns>
        protected abstract bool UpdateAdornment(TAdornment adornment, TData data);

        /// <param name="spans">Spans to provide adornment data for. These spans do not necessarily correspond to text lines.</param>
        /// <remarks>
        /// If adornments need to be updated, call <see cref="RaiseTagsChanged"/> or <see cref="InvalidateSpans"/>.
        /// This will, indirectly, cause <see cref="GetAdornmentData"/> to be called.
        /// </remarks>
        /// <returns>
        /// A sequence of:
        ///  * adornment data for each adornment to be displayed
        ///  * the span of text that should be elided for that adornment (zero length spans are acceptable)
        ///  * and affinity of the adornment (this should be null if and only if the elided span has a length greater than zero)
        /// </returns>
        protected abstract IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, TData>> GetAdornmentData(NormalizedSnapshotSpanCollection spans);

        private void HandleBufferChanged(object sender, TextContentChangedEventArgs args)
        {
            var editedSpans = args.Changes.Select(change => new SnapshotSpan(args.After, change.NewSpan)).ToList();
            InvalidateSpans(editedSpans);
        }

        /// <summary>
        /// Causes intra-text adornments to be updated asynchronously.
        /// </summary>
        protected void InvalidateSpans(IList<SnapshotSpan> spans)
        {
            lock (_invalidatedSpans)
            {
                var wasEmpty = _invalidatedSpans.Count == 0;
                _invalidatedSpans.AddRange(spans);

                if (wasEmpty && _invalidatedSpans.Count > 0)
                {
                    _view.VisualElement.BeginInvokeOnMainThread(new Action(AsyncUpdate));
                    //_view.VisualElement.Dispatcher.BeginInvoke(new Action(AsyncUpdate));
                }
            }
        }

        void AsyncUpdate()
        {
            // Store the snapshot that we're now current with and send an event
            // for the text that has changed.
            if (Snapshot != _view.TextBuffer.CurrentSnapshot)
            {
                Snapshot = _view.TextBuffer.CurrentSnapshot;

                var translatedAdornmentCache = new Dictionary<SnapshotSpan, TAdornment>();

                foreach (var keyValuePair in _adornmentCache)
                    translatedAdornmentCache.Add(keyValuePair.Key.TranslateTo(Snapshot, SpanTrackingMode.EdgeExclusive), keyValuePair.Value);

                _adornmentCache = translatedAdornmentCache;
            }

            List<SnapshotSpan> translatedSpans;
            lock (_invalidatedSpans)
            {
                translatedSpans = _invalidatedSpans.Select(s => s.TranslateTo(Snapshot, SpanTrackingMode.EdgeInclusive)).ToList();
                _invalidatedSpans.Clear();
            }

            if (translatedSpans.Count == 0)
                return;

            var start = translatedSpans.Select(span => span.Start).Min();
            var end = translatedSpans.Select(span => span.End).Max();

            RaiseTagsChanged(new SnapshotSpan(start, end));
        }

        /// <summary>
        /// Causes intra-text adornments to be updated synchronously.
        /// </summary>
        protected void RaiseTagsChanged(SnapshotSpan span)
        {
            var handler = TagsChanged;
            if (handler != null)
                handler(this, new SnapshotSpanEventArgs(span));
        }

        void HandleLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            var visibleSpan = _view.TextViewLines.FormattedSpan;

            // Filter out the adornments that are no longer visible.
            var toRemove = new List<SnapshotSpan>(
                from keyValuePair
                in _adornmentCache
                where !keyValuePair.Key.TranslateTo(visibleSpan.Snapshot, SpanTrackingMode.EdgeExclusive).IntersectsWith(visibleSpan)
                select keyValuePair.Key);

            foreach (var span in toRemove)
                _adornmentCache.Remove(span);
        }


        // Produces tags on the snapshot that the tag consumer asked for.
        public virtual IEnumerable<ITagSpan<XPlatIntraTextAdornmentTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            // Translate the request to the snapshot that this tagger is current with.

            var requestedSnapshot = spans[0].Snapshot;

            var translatedSpans = new NormalizedSnapshotSpanCollection(spans.Select(span => span.TranslateTo(Snapshot, SpanTrackingMode.EdgeExclusive)));

            // Grab the adornments.
            foreach (var tagSpan in GetAdornmentTagsOnSnapshot(translatedSpans))
            {
                // Translate each adornment to the snapshot that the tagger was asked about.
                var span = tagSpan.Span.TranslateTo(requestedSnapshot, SpanTrackingMode.EdgeExclusive);
                var tag = new XPlatIntraTextAdornmentTag(tagSpan.Tag.Adornment, tagSpan.Tag.RemovalCallback, tagSpan.Tag.Affinity);
                yield return new TagSpan<XPlatIntraTextAdornmentTag>(span, tag);
            }
        }

        // Produces tags on the snapshot that this tagger is current with.
        private IEnumerable<TagSpan<XPlatIntraTextAdornmentTag>> GetAdornmentTagsOnSnapshot(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0)
                yield break;

            var snapshot = spans[0].Snapshot;

            System.Diagnostics.Debug.Assert(snapshot == this.Snapshot);

            // Since WPF UI objects have state (like mouse hover or animation) and are relatively expensive to create and lay out,
            // this code tries to reuse controls as much as possible.
            // The controls are stored in this.adornmentCache between the calls.

            // Mark which adornments fall inside the requested spans with Keep=false
            // so that they can be removed from the cache if they no longer correspond to data tags.
            var toRemove = new HashSet<SnapshotSpan>();
            foreach (var ar in _adornmentCache)
                if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(ar.Key)))
                    toRemove.Add(ar.Key);

            foreach (var spanDataPair in GetAdornmentData(spans).Distinct(new Comparer()))
            {
                // Look up the corresponding adornment or create one if it's new.
                TAdornment adornment;
                var snapshotSpan = spanDataPair.Item1;
                var affinity = spanDataPair.Item2;
                var adornmentData = spanDataPair.Item3;
                if (_adornmentCache.TryGetValue(snapshotSpan, out adornment))
                {
                    if (UpdateAdornment(adornment, adornmentData))
                        toRemove.Remove(snapshotSpan);
                }
                else
                {
                    adornment = CreateAdornment(adornmentData, snapshotSpan);

                    if (adornment == null)
                        continue;

                    // Get the adornment to measure itself. Its DesiredSize property is used to determine
                    // how much space to leave between text for this adornment.
                    // Note: If the size of the adornment changes, the line will be reformatted to accommodate it.
                    // Note: Some adornments may change size when added to the view's visual tree due to inherited
                    // dependency properties that affect layout. Such options can include SnapsToDevicePixels,
                    // UseLayoutRounding, TextRenderingMode, TextHintingMode, and TextFormattingMode. Making sure
                    // that these properties on the adornment match the view's values before calling Measure here
                    // can help avoid the size change and the resulting unnecessary re-format.

                    //TODO: Figure this out
                    //adornment.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                    _adornmentCache.Add(snapshotSpan, adornment);
                }

                yield return new TagSpan<XPlatIntraTextAdornmentTag>(snapshotSpan, new XPlatIntraTextAdornmentTag(adornment, null, affinity));
            }

            foreach (var snapshotSpan in toRemove)
                _adornmentCache.Remove(snapshotSpan);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        class Comparer : IEqualityComparer<Tuple<SnapshotSpan, PositionAffinity?, TData>>
        {
            public bool Equals(Tuple<SnapshotSpan, PositionAffinity?, TData> x, Tuple<SnapshotSpan, PositionAffinity?, TData> y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null || y == null)
                    return false;
                return x.Item1.Equals(y.Item1);
            }

            public int GetHashCode(Tuple<SnapshotSpan, PositionAffinity?, TData> obj)
            {
                return obj.Item1.GetHashCode();
            }
        }

    }
}
