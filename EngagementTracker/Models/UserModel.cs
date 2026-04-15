namespace EngagementTracker.Models;

public class UserModel
{
    public string Uid     { get; set; } = "";
    public string Name    { get; set; } = "";
    public string Email   { get; set; } = "";
    public string Role    { get; set; } = "";
    public string Section { get; set; } = "";
    public string RollNo  { get; set; } = "";
    public string Password { get; set; } = "";
    public string PhotoData { get; set; } = "";
}

public class LoginViewModel
{
    public string Email        { get; set; } = "";
    public string Password     { get; set; } = "";
    public string ErrorMessage { get; set; } = "";
}

public class RegisterViewModel
{
    public string Uid          { get; set; } = "";
    public string Name         { get; set; } = "";
    public string Email        { get; set; } = "";
    public string Role         { get; set; } = "";
    public string Section      { get; set; } = "";
    public string RollNo       { get; set; } = "";
    public string Department   { get; set; } = "";
    public string EnrollmentNo { get; set; } = "";
    public string PhotoData    { get; set; } = "";
}