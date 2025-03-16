public static class UserSession
{
    // Unique teacher ID is set on teacher login. For students, this remains empty.
    public static string teacherId = "";

    // When a group is selected/created, its key is stored here.
    public static string selectedGroupKey = "";

    // Username of the logged-in student or teacher.
    public static string username = "";

    // Organization type ("As a Student" or "As a Teacher").
    public static string organization = "";
}
