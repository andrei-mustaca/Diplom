using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Domain.Models;

public class User
{
    public Guid Id{get;set;}
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
    public UserRole Role { get; set; }
    
    
    public List<RequestAssigment> RequestAssigments { get; set; }
    public List<WorkReport> WorkReports { get; set; }
}