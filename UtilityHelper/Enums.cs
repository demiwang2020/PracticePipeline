namespace Helper
{
    /// <summary>
    /// Enum reflecting all supported architectures
    /// </summary>
    public enum Architecture
    {
        X86=1,
        AMD64=2,
        IA64=3,
        ARM=4,
        ARM64=5
    }

    public enum RunStatus { Running = 1, Completed = 2, NotStarted = 3, Pending = 4, Analyzing = 5, Error = 6, Abandon = 7 };
    public enum RunResult { Unknown = 1, Passed = 2, Failed = 3, Pending = 4, Error = 5 };

    public enum EnumPrivateTestStepType
    {
        Info,
        Test
    }

    public enum RefreshRedistHFRType
    {
        FullRedist,
        RefreshRedistMSU,
        FullRedistISV,
        Webbootstrapper
    }


}
