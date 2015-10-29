using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using NuGetConsole.Implementation.Console;
using Xunit;

namespace Console.Test
{
    public class WpfConsoleClassifierTests
    {
        private class MockClassificationType : IClassificationType
        {
            public MockClassificationType()
            {
                Classification = TextFormatClassifier.GetClassificationName(Colors.Red, Colors.White);
            }

            public IEnumerable<IClassificationType> BaseTypes
            {
                get
                {
                    return Enumerable.Empty<IClassificationType>();
                }
            }

            public string Classification { get; }

            public bool IsOfType(string type)
            {
                return string.Equals(Classification, type, StringComparison.Ordinal);
            }
        }

        private class MockServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                return null;
            }
        }

        [Fact]
        public void WpfConsoleClassifier_GetClassificationSpans_Fail()
        {
            // Arrange
            var wpfConsoleService = new WpfConsoleService();
            var textBufferHelper = new TestTextBufferHelper();
            var textBuffer = textBufferHelper.TextBufferFactory.CreateTextBuffer();
            textBuffer.Insert(0, "One1Two2");

            var wpfConsoleClassifer = new WpfConsoleClassifier(wpfConsoleService, textBuffer);

            var span1 = new Span(6, 4);
            var mockClassificationType = new MockClassificationType();

            wpfConsoleClassifer._colorSpans.Add(
                new Tuple<Span, IClassificationType>(span1, mockClassificationType));

            var serviceProvider = new MockServiceProvider();
            wpfConsoleClassifer._console
                = new WpfConsole(wpfConsoleService, serviceProvider, null, null, null);

            var span = new Span(5, 2);
            var snapShotSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, span);

            // Act
            var classificationSpans = wpfConsoleClassifer.GetClassificationSpans(snapShotSpan);

            // Assert
            Assert.Equal(1, classificationSpans.Count);
        }
    }
}
