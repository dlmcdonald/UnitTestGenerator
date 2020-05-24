using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace UnitTestGenerator.Mac.Glyphs
{
    public class TodoTagger : ITagger<TodoTag>
    {
        readonly IClassifier _classifier;
        const string _searchText = "todo";

        public TodoTagger(IClassifier classifier)
        {
            _classifier = classifier;
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<TodoTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                //look at each classification span \
                foreach (var classification in _classifier.GetClassificationSpans(span))
                {
                    if (span.GetText().ToLower().Contains("handle"))
                    {
                        Debug.WriteLine("Found that");
                    }
                    //if the classification is a comment
                    if (classification.ClassificationType.Classification.ToLower().Contains("comment"))
                    {
                        //if the word "todo" is in the comment,
                        //create a new TodoTag TagSpan
                        var index = classification.Span.GetText().ToLower().IndexOf(_searchText);
                        if (index != -1)
                        {
                            yield return new TagSpan<TodoTag>(new SnapshotSpan(classification.Span.Start + index, _searchText.Length), new TodoTag());
                        }
                    }
                }
            }
        }
    }
}
