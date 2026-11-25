namespace StudentRegistry;

internal sealed record StudentDetails(string FullName, string GroupName, int Course, string Email);

internal sealed record Student(long Id, StudentDetails Details);
