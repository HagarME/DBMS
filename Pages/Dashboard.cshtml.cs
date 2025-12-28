using Microsoft.AspNetCore.Mvc.RazorPages;
using dashProject.Models;
using System.Data;
using System;

namespace dashProject.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly EFMS DB;

        public DashboardModel(EFMS db)
        {
            DB = db;
        }

        public int TotalClients { get; set; }
        public int TotalEmployees { get; set; }
        public int OpenProjects { get; set; }
        public int PendingTasks { get; set; }
        public int UnreadEmails { get; set; }
        public int OfficeRequests { get; set; }
        public DataTable RecentActivities { get; set; }

        // Role-based properties
        public string UserRole { get; set; }
        public string UserDeptId { get; set; }

        public void OnGet()
        {
            // Get user role and department from session
            UserRole = HttpContext.Session.GetString("UserRole")?.ToLower() ?? "employee";
            UserDeptId = HttpContext.Session.GetString("UserDeptId") ?? "0";

            // ADMIN VIEW - All data
            if (UserRole == "admin")
            {
                TotalClients = DB.GetTotalClients();
                TotalEmployees = DB.GetTotalEmployees();
                OpenProjects = DB.GetOpenProjects();
                PendingTasks = DB.GetPendingTasks();
                UnreadEmails = DB.GetUnreadEmails();
                OfficeRequests = DB.GetOfficeRequestsCount();
                RecentActivities = DB.GetRecentActivities();
            }
            // MANAGER VIEW - Department-specific data
            else if (UserRole == "manager")
            {
                TotalClients = DB.GetTotalClientsForDepartment(UserDeptId);
                TotalEmployees = DB.GetTotalEmployeesForDepartment(UserDeptId);
                OpenProjects = DB.GetOpenProjectsForDepartment(UserDeptId);
                PendingTasks = DB.GetPendingTasksForDepartment(UserDeptId);
                UnreadEmails = DB.GetUnreadEmails(); // Same for all
                OfficeRequests = DB.GetOfficeRequestsForDepartment(UserDeptId);
                RecentActivities = DB.GetRecentActivitiesForDepartment(UserDeptId);
            }
        }
    }
}