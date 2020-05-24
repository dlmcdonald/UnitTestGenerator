using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace UnitTestGenerator.Mac.Glyphs
{
    public class TestableMethodTagger : ITagger<TestableMethodTag>
    {
        readonly IClassifier _classifier;
        const string _searchText = "public";

        public TestableMethodTagger(IClassifier classifier)
        {
            _classifier = classifier;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<TestableMethodTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                //look at each classification span \
                foreach (var classification in _classifier.GetClassificationSpans(span))
                {
                    //if the classification is a comment
                    if (classification.ClassificationType.Classification.ToLower().Contains("method name"))
                    {
                        //if the word "todo" is in the comment,
                        //create a new TodoTag TagSpan
                        var index = span.GetText().ToLower().IndexOf(_searchText);
                        if (index != -1)
                        {
                            yield return new TagSpan<TestableMethodTag>(new SnapshotSpan(classification.Span.Start + index, _searchText.Length), new TestableMethodTag());
                        }
                    }
                }
            }
        }
    }
}
