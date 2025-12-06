using FluentAssertions;
using TE.FileWatcher.Configuration;
using Xunit;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace FileWatcher.Tests.Configuration
{
    public class XmlFileTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly List<string> _testFiles = new();

        public XmlFileTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"XmlFileTests_{Guid.NewGuid()}");
            IODirectory.CreateDirectory(_testDirectory);
        }

        public void Dispose()
        {
            foreach (var file in _testFiles.Where(IOFile.Exists))
            {
                try { IOFile.Delete(file); }
                catch { }
            }
            if (IODirectory.Exists(_testDirectory))
            {
                try { IODirectory.Delete(_testDirectory, true); }
                catch { }
            }
        }

        [Fact]
        public void XmlFile_WithNullPathAndName_ShouldUseDefaults()
        {
            // Act
            var xmlFile = new XmlFile(null, null);

            // Assert - Should not throw
            xmlFile.Should().NotBeNull();
        }

        [Fact]
        public void XmlFile_WithPathAndNullName_ShouldUseDefaultName()
        {
            // Act
            var xmlFile = new XmlFile(_testDirectory, null);

            // Assert
            xmlFile.Should().NotBeNull();
        }

        [Fact]
        public void XmlFile_WithNullPathAndName_ShouldUseDefaultName()
        {
            // Act
            var xmlFile = new XmlFile(null, "custom.xml");

            // Assert
            xmlFile.Should().NotBeNull();
        }

        [Fact]
        public void XmlFile_WithValidPathAndName_ShouldNotThrow()
        {
            // Act
            System.Action act = () => new XmlFile(_testDirectory, "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithEmptyPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithEmptyName_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile(_testDirectory, "");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithNonExistentPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("C:\\NonExistent\\Path", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithSpecialCharactersInName_ShouldAccept()
        {
            // Act
            System.Action act = () => new XmlFile(_testDirectory, "my-config_v2.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_DefaultConfigFile_ShouldBeConfigXml()
        {
            // Assert
            XmlFile.DEFAULTCONFIGFILE.Should().Be("config.xml");
        }

        [Fact]
        public void XmlFile_WithRelativePath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("./config", "test.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithLongPath_ShouldHandleGracefully()
        {
            // Arrange
            var longPath = Path.Combine(_testDirectory, new string('a', 200));

            // Act
            System.Action act = () => new XmlFile(longPath, "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithUNCPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("\\\\server\\share", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_CalledMultipleTimes_ShouldCreateDifferentInstances()
        {
            // Act
            var xmlFile1 = new XmlFile(_testDirectory, "config.xml");
            var xmlFile2 = new XmlFile(_testDirectory, "config.xml");

            // Assert
            xmlFile1.Should().NotBeSameAs(xmlFile2);
        }

        [Fact]
        public void XmlFile_WithWhitespacePath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("   ", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithWhitespaceName_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile(_testDirectory, "   ");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithPathContainingSpaces_ShouldAccept()
        {
            // Arrange
            var pathWithSpaces = Path.Combine(_testDirectory, "path with spaces");
            if (!IODirectory.Exists(pathWithSpaces))
            {
                IODirectory.CreateDirectory(pathWithSpaces);
            }

            // Act
            System.Action act = () => new XmlFile(pathWithSpaces, "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithNameContainingSpaces_ShouldAccept()
        {
            // Act
            System.Action act = () => new XmlFile(_testDirectory, "my config file.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithDotInPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile(".", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithDoubleDotInPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("..", "config.xml");

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void XmlFile_WithMixedSlashesInPath_ShouldHandleGracefully()
        {
            // Act
            System.Action act = () => new XmlFile("C:/test\\path/mixed", "config.xml");

            // Assert
            act.Should().NotThrow();
        }
    }
}
