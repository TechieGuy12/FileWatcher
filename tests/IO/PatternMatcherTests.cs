using FluentAssertions;
using TE.FileWatcher.IO;
using Xunit;

namespace FileWatcher.Tests.IO
{
    public class PatternMatcherTests
    {
        [Theory]
        [InlineData("*", "anyfile.txt", true)]
        [InlineData("*.*", "anyfile.txt", true)]
        [InlineData("*.txt", "test.txt", true)]
        [InlineData("*.txt", "test.doc", false)]
        [InlineData("test.*", "test.txt", true)]
        [InlineData("test.*", "testing.txt", false)]
        [InlineData("test?.txt", "test1.txt", true)]
        [InlineData("test?.txt", "test12.txt", false)]
        [InlineData("test*.txt", "test.txt", true)]
        [InlineData("test*.txt", "test123.txt", true)]
        [InlineData("test*.txt", "test.doc", false)]
        public void StrictMatchPattern_WithVariousPatterns_ShouldMatchCorrectly(
            string pattern, string filename, bool expectedMatch)
        {
            // Act
            var result = PatternMatcher.StrictMatchPattern(pattern, filename);

            // Assert
            result.Should().Be(expectedMatch);
        }

        [Theory]
        [InlineData("", "file.txt")]
        [InlineData("*.txt", "")]
        [InlineData(null, "file.txt")]
        [InlineData("*.txt", null)]
        public void StrictMatchPattern_WithEmptyOrNullInputs_ShouldReturnFalse(
            string pattern, string filename)
        {
            // Act
            var result = PatternMatcher.StrictMatchPattern(pattern, filename);

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("*.TXT", "file.txt", true)]
        [InlineData("*.txt", "FILE.TXT", true)]
        public void StrictMatchPattern_WithDifferentCasing_ShouldBeCaseInsensitive(
            string pattern, string filename, bool expectedMatch)
        {
            // Act
            var result = PatternMatcher.StrictMatchPattern(pattern, filename);

            // Assert
            result.Should().Be(expectedMatch);
        }

        [Theory]
        [InlineData("*.log", "app.log", true)]
        [InlineData("*.log", "app.log.bak", false)]
        [InlineData("app.*", "app.config", true)]
        [InlineData("app.*", "application.config", false)]
        public void StrictMatchPattern_WithExactPatterns_ShouldMatchExactly(
            string pattern, string filename, bool expectedMatch)
        {
            // Act
            var result = PatternMatcher.StrictMatchPattern(pattern, filename);

            // Assert
            result.Should().Be(expectedMatch);
        }

        [Theory]
        [InlineData("file*", "file", true)]
        [InlineData("file*", "file.txt", true)]
        [InlineData("file*", "filename.txt", true)]
        [InlineData("*file", "myfile", true)]
        [InlineData("*file", "file", true)]
        [InlineData("*file*", "myfiletxt", true)]
        public void StrictMatchPattern_WithWildcardAtDifferentPositions_ShouldMatch(
            string pattern, string filename, bool expectedMatch)
        {
            // Act
            var result = PatternMatcher.StrictMatchPattern(pattern, filename);

            // Assert
            result.Should().Be(expectedMatch);
        }
    }
}
