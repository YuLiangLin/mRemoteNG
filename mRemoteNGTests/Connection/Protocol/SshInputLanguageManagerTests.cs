using System;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol
{
    public class SshInputLanguageManagerTests
    {
        [Test]
        public void FindEnglishLayout_ReturnsEnglishUnitedStatesLayout()
        {
            IntPtr englishLayout = new(unchecked((long)0xF0010409));
            IntPtr[] layouts =
            [
                new(unchecked((long)0xF0010404)),
                englishLayout,
                new(unchecked((long)0xF0010411))
            ];

            IntPtr result = SshInputLanguageManager.FindEnglishLayout(layouts);

            Assert.That(result, Is.EqualTo(englishLayout));
        }

        [Test]
        public void FindEnglishLayout_ReturnsZeroWhenEnglishIsNotInstalled()
        {
            IntPtr[] layouts =
            [
                new(unchecked((long)0xF0010404)),
                new(unchecked((long)0xF0010411))
            ];

            IntPtr result = SshInputLanguageManager.FindEnglishLayout(layouts);

            Assert.That(result, Is.EqualTo(IntPtr.Zero));
        }
    }
}
