using Microsoft.Data.SqlClient;
using System.Data;
using System.Transactions;

namespace dashProject.Models
{
    public class EFMS
    {
        private readonly string ConnectionString =
            "Server=SQL6032.site4now.net,1433;" +
            "Database=db_ac28c5_m0hamedibrahim;" +
            "User Id=db_ac28c5_m0hamedibrahim_admin;" +
            "Password=MO-01091707714;" +
            "Encrypt=True;" +
            "TrustServerCertificate=True;";
        
        public SqlConnection con { get; set; }
        
        public EFMS()
        {
            con = new SqlConnection(ConnectionString);
        }

        public DataRow ValidateUser(string email, string password)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Employee WHERE email = @Email AND password = @Password";
            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@Password", password);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public (bool Success, string ErrorMessage) SaveIncomingEmail(string sender, string subject, string body, string recipient, DateTime date)
        {
            string query = @"
                INSERT INTO E_Case_Source (sender_email, subject, body, recipient_email, date, status)
                VALUES (@Sender, @Subject, @Body, @Recipient, @Date, 'Unread')";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Sender", sender);
                    cmd.Parameters.AddWithValue("@Subject", subject ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Body", body ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Recipient", recipient);
                    cmd.Parameters.AddWithValue("@Date", date);
                    int rows = cmd.ExecuteNonQuery();
                    return (rows > 0, rows > 0 ? null : "No rows affected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving email: {ex.Message}");
                return (false, ex.Message);
            }
            finally
            {
                con.Close();
            }
        }

        public DataTable ReadTable(string Table)
        {
            DataTable dt = new DataTable();
            string Q = $"SELECT * FROM {Table}";
            try
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(Q, con);
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        
        public DataTable SearchEmployeeBySSN(string ssn)
        {
            DataTable dt = new DataTable();
            if (string.IsNullOrWhiteSpace(ssn))
            {
                return dt;
            }

            string query = "SELECT * FROM Employee WHERE ssn = @Ssn";
            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        public int GetEmployeeCount()
        {
            int count = 0;
            string query = "SELECT COUNT(*) FROM Employee";
            try
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                count = (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return count;
        }

        public bool AddEmployee(string ssn, string fname, string minit, string lname, 
                               string email, string phoneNumber, string role, 
                               int? deptId, string superSsn, string password)
        {
            string query = @"INSERT INTO Employee (ssn, Fname, Minit, Lname, email, phone_number, role, dept_id, super_ssn, password) 
                           VALUES (@Ssn, @Fname, @Minit, @Lname, @Email, @PhoneNumber, @Role, @DeptId, @SuperSsn, @Password)";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.Parameters.AddWithValue("@Fname", (object)fname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Minit", (object)minit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lname", (object)lname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", (object)role ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeptId", (object)deptId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SuperSsn", (object)superSsn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object)password ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        // ================= PENDING ACCOUNTS =================

        public bool RequestSignUp(string fullName, string email, string password, string role)
        {
            string query = "INSERT INTO PendingAccounts (FullName, Email, Password, Role) VALUES (@Name, @Email, @Pass, @Role)";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Name", fullName);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Pass", password);
            cmd.Parameters.AddWithValue("@Role", role);
            try
            {
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
            finally { con.Close(); }
        }

        public DataTable GetPendingAccounts()
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM PendingAccounts WHERE Status = 'Pending' ORDER BY RequestDate DESC";
            try
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(query, con);
                da.Fill(dt);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { con.Close(); }
            return dt;
        }

        public DataRow GetPendingAccountById(int requestId)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM PendingAccounts WHERE RequestID = @ID";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", requestId);
            try
            {
                con.Open();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                if (dt.Rows.Count > 0) return dt.Rows[0];
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { con.Close(); }
            return null;
        }

        public bool UpdatePendingAccount(int requestId, string fullName, string email, string password, string role)
        {
            string query = "UPDATE PendingAccounts SET FullName=@Name, Email=@Email, Password=@Pass, Role=@Role WHERE RequestID=@ID";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", requestId);
            cmd.Parameters.AddWithValue("@Name", fullName);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Pass", password);
            cmd.Parameters.AddWithValue("@Role", role);
            try
            {
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
            finally { con.Close(); }
        }

        public bool DeletePendingAccount(int requestId)
        {
            string query = "DELETE FROM PendingAccounts WHERE RequestID = @ID";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@ID", requestId);
            try
            {
                con.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
            finally { con.Close(); }
        }

        public bool ApproveAccount(int requestId, string ssn, string fname, string minit, string lname, 
                                 string email, string phone, string role, int? deptId, 
                                 string superSsn, string password)
        {
            // Use a transaction to ensure both operations succeed or fail together
            try
            {
                con.Open();
                using SqlTransaction trans = con.BeginTransaction();
                try
                {
                    // 1. Insert into Employee
                    string insertQuery = @"INSERT INTO Employee (ssn, Fname, Minit, Lname, email, phone_number, role, dept_id, super_ssn, password) 
                                         VALUES (@Ssn, @Fname, @Minit, @Lname, @Email, @Phone, @Role, @DeptId, @SuperSsn, @Pass)";
                    using SqlCommand cmdInsert = new SqlCommand(insertQuery, con, trans);
                    cmdInsert.Parameters.AddWithValue("@Ssn", ssn);
                    cmdInsert.Parameters.AddWithValue("@Fname", (object)fname ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Minit", (object)minit ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Lname", (object)lname ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Phone", (object)phone ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Role", (object)role ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@DeptId", (object)deptId ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@SuperSsn", (object)superSsn ?? DBNull.Value);
                    cmdInsert.Parameters.AddWithValue("@Pass", (object)password ?? DBNull.Value);
                    cmdInsert.ExecuteNonQuery();

                    // 2. Delete from PendingAccounts
                    string deleteQuery = "DELETE FROM PendingAccounts WHERE RequestID = @ID";
                    using SqlCommand cmdDelete = new SqlCommand(deleteQuery, con, trans);
                    cmdDelete.Parameters.AddWithValue("@ID", requestId);
                    cmdDelete.ExecuteNonQuery();

                    trans.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); return false; }
            finally { con.Close(); }
        }

        public DataRow GetEmployeeBySSN(string ssn)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Employee WHERE ssn = @Ssn";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public bool UpdateEmployee(string ssn, string fname, string minit, string lname,
                                  string email, string phoneNumber, string role,
                                  int? deptId, string superSsn)
        {
            string query = @"UPDATE Employee 
                           SET Fname = @Fname, 
                               Minit = @Minit, 
                               Lname = @Lname, 
                               email = @Email, 
                               phone_number = @PhoneNumber, 
                               role = @Role, 
                               dept_id = @DeptId, 
                               super_ssn = @SuperSsn 
                           WHERE ssn = @Ssn";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.Parameters.AddWithValue("@Fname", (object)fname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Minit", (object)minit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Lname", (object)lname ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Role", (object)role ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@DeptId", (object)deptId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@SuperSsn", (object)superSsn ?? DBNull.Value);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        public void DeleteEmployee(string ssn)
        {
            SqlTransaction? transaction = null;
            try
            {
                con.Open();
                transaction = con.BeginTransaction();
                
                string clearSuper = "UPDATE Employee SET Super_ssn = NULL WHERE Super_ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(clearSuper, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                string clearManager = "UPDATE Department SET Mgr_ssn = NULL WHERE Mgr_ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(clearManager, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                string deleteWorksOn = "DELETE FROM Works_On WHERE emp_ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(deleteWorksOn, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                string deleteSubmissions = "DELETE FROM Submission WHERE emp_ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(deleteSubmissions, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                string deleteAssigns = "DELETE FROM Assigns WHERE e_ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(deleteAssigns, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                string deleteEmp = "DELETE FROM Employee WHERE Ssn = @Ssn";
                using (SqlCommand cmd = new SqlCommand(deleteEmp, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@Ssn", ssn);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public DataTable GetAllEmployeesForDropdown()
        {
            DataTable dt = new DataTable();
            // FILTER LOGIC: Only show employees who have data in Assigns OR Filter (emails)
            string query = @"
                SELECT DISTINCT e.ssn, e.Fname, e.Lname 
                FROM Employee e
                LEFT JOIN Assigns a ON e.ssn = a.e_ssn
                LEFT JOIN E_Case_Source mail ON e.email = mail.recipient_email
                WHERE a.task_id IS NOT NULL OR mail.email_id IS NOT NULL
                ORDER BY e.Fname, e.Lname";

            try
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }


        public DataTable SearchDepartmentById(int deptId)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Department WHERE dept_id = @DeptId";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        public DataTable SearchDepartmentBySpecialization(string specialization)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Department WHERE specialization LIKE @Specialization";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@Specialization", "%" + specialization + "%");
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        public bool AddDepartment(string specialization, string description, string mgrSsn)
        {
            int newDeptId;
            SqlTransaction transaction = null;

            try
            {
                con.Open();
                transaction = con.BeginTransaction(System.Data.IsolationLevel.Serializable);

                // 1. Generate new ID
                string getNewId = "SELECT ISNULL(MAX(dept_id), 0) + 1 FROM Department WITH (UPDLOCK, HOLDLOCK)";
                using (SqlCommand cmd = new SqlCommand(getNewId, con, transaction))
                {
                    newDeptId = (int)cmd.ExecuteScalar();
                }

                // 2. Insert with new ID
                string query = @"INSERT INTO Department (dept_id, specialization, description, mgr_ssn) 
                               VALUES (@DeptId, @Specialization, @Description, @MgrSsn)";

                using (SqlCommand cmd = new SqlCommand(query, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", newDeptId);
                    cmd.Parameters.AddWithValue("@Specialization", (object)specialization ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object)description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MgrSsn", string.IsNullOrWhiteSpace(mgrSsn) ? DBNull.Value : (object)mgrSsn);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        public DataRow GetDepartmentById(int deptId)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Department WHERE dept_id = @DeptId";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public bool UpdateDepartment(int deptId, string specialization, string description, string mgrSsn)
        {
            string query = @"UPDATE Department 
                           SET specialization = @Specialization, 
                               description = @Description, 
                               mgr_ssn = @MgrSsn 
                           WHERE dept_id = @DeptId";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.Parameters.AddWithValue("@Specialization", (object)specialization ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Description", (object)description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@MgrSsn", string.IsNullOrWhiteSpace(mgrSsn) ? DBNull.Value : (object)mgrSsn);

                    return cmd.ExecuteNonQuery() > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                con.Close();
            }
        }

        public void DeleteDepartment(int deptId)
        {
            SqlTransaction? transaction = null;
            try
            {
                con.Open();
                transaction = con.BeginTransaction();

                string clearEmployees = "UPDATE Employee SET dept_id = NULL WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(clearEmployees, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.ExecuteNonQuery();
                }

                string deleteAssigns = "DELETE FROM Assigns WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(deleteAssigns, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.ExecuteNonQuery();
                }

                string deleteDocuments = "DELETE FROM Documents WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(deleteDocuments, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.ExecuteNonQuery();
                }

                string updateProjects = "UPDATE Project SET dept_id = NULL WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(updateProjects, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.ExecuteNonQuery();
                }

                string deleteDept = "DELETE FROM Department WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(deleteDept, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        public DataTable GetAdminEmployees()
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Employee WHERE role = 'Admin' AND email_alerts = 1";
            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { con.Close(); }
            return dt;
        }

        public DataRow GetDepartmentManager(int deptId)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT e.* 
                FROM Employee e
                JOIN Department d ON e.ssn = d.mgr_ssn
                WHERE d.dept_id = @DeptId AND e.email_alerts = 1";
            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { con.Close(); }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable GetAllDepartmentsForDropdown()
        {
            DataTable dt = new DataTable();
            string query = "SELECT dept_id, specialization FROM Department ORDER BY specialization";

            try
            {
                con.Open();
                SqlCommand cmd = new SqlCommand(query, con);
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }


        public DataTable ReadTwoTables(string table1, string table2, string joinColumn)
        {
            DataTable dt = new DataTable();
            string Q = $@"
                SELECT t1.*, t2.*
                FROM {table1} t1
                INNER JOIN {table2} t2
                    ON t1.{joinColumn} = t2.{joinColumn}
            ";

            try
            {
                con.Open();
                using SqlCommand cmd = new SqlCommand(Q, con);
                dt.Load(cmd.ExecuteReader());
            }
            finally
            {
                con.Close();
            }

            return dt;
        }

        public int AddDocument(string title, string content, int deptId, string empSsn, byte[] fileContent, string originalFileName, string contentType)
        {
            int newDocId;

            SqlTransaction transaction = null;

            try
            {
                con.Open();
                transaction = con.BeginTransaction(System.Data.IsolationLevel.Serializable);

                // 1️⃣ Generate new doc_id safely
                string getNewId = @"
            SELECT ISNULL(MAX(doc_id), 0) + 1 
            FROM Documents WITH (UPDLOCK, HOLDLOCK);
        ";

                using (SqlCommand idCmd = new SqlCommand(getNewId, con, transaction))
                {
                    newDocId = (int)idCmd.ExecuteScalar();
                }

                // 2️⃣ Insert into Documents with BLOB
                string insertDoc = @"
            INSERT INTO Documents (doc_id, title, content, dept_id, file_content, original_file_name, file_type)
            VALUES (@doc_id, @title, @content, @dept_id, @fileContent, @originalFileName, @contentType);
        ";

                using (SqlCommand cmd = new SqlCommand(insertDoc, con, transaction))
                {
                    cmd.Parameters.Add("@doc_id", SqlDbType.Int).Value = newDocId;
                    cmd.Parameters.Add("@title", SqlDbType.NVarChar).Value = title;
                    cmd.Parameters.Add("@content", SqlDbType.NVarChar).Value = content;
                    cmd.Parameters.Add("@dept_id", SqlDbType.Int).Value = deptId;
                    
                    // Handle NULL file content
                    SqlParameter paramFile = new SqlParameter("@fileContent", SqlDbType.VarBinary);
                    paramFile.Value = fileContent ?? (object)DBNull.Value;
                    if (fileContent != null) paramFile.Size = -1; // MAX
                    cmd.Parameters.Add(paramFile);

                    cmd.Parameters.Add("@originalFileName", SqlDbType.NVarChar).Value = (object)originalFileName ?? DBNull.Value;
                    cmd.Parameters.Add("@contentType", SqlDbType.NVarChar).Value = (object)contentType ?? DBNull.Value;
                    
                    cmd.ExecuteNonQuery();
                }

                // 3️⃣ Insert into Submission
                string insertSubmission = @"
            INSERT INTO Submission (doc_id, emp_ssn, submission_date)
            VALUES (@doc_id, @emp_ssn, GETDATE());
        ";

                using (SqlCommand cmd = new SqlCommand(insertSubmission, con, transaction))
                {
                    cmd.Parameters.Add("@doc_id", SqlDbType.Int).Value = newDocId;
                    cmd.Parameters.Add("@emp_ssn", SqlDbType.VarChar).Value = empSsn;
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                con.Close();
            }

            return newDocId;
        }


        // --- Get Document Attachment ---
        public (byte[] Content, string FileName, string ContentType) GetDocumentAttachment(int docId)
        {
            byte[] content = null;
            string fileName = "download";
            string contentType = "application/octet-stream";

            string query = "SELECT file_content, original_file_name, file_type FROM Documents WHERE doc_id = @id";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", docId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (reader["file_content"] != DBNull.Value)
                                content = (byte[])reader["file_content"];
                            
                            if (reader["original_file_name"] != DBNull.Value)
                                fileName = reader["original_file_name"].ToString();

                            if (reader["file_type"] != DBNull.Value)
                                contentType = reader["file_type"].ToString();
                        }
                    }
                }
            }
            finally
            {
                con.Close();
            }

            return (content, fileName, contentType);
        }
        public DataTable SearchDocuments(string keyword)
        {
            DataTable dt = new DataTable();
            string query = @"
        SELECT d.doc_id, d.title, d.content, d.dept_id,
               s.emp_ssn, s.submission_date
        FROM Documents d
        LEFT JOIN Submission s ON d.doc_id = s.doc_id
        WHERE d.doc_id LIKE @kw OR d.title LIKE @kw
    ";

            try
            {
                con.Open();
                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@kw", $"%{keyword}%");
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }

            return dt;
        }

        // Delete Document by doc_id // 
        public void DeleteDocument(int docId)
        {
            SqlTransaction? tx = null;

            try
            {
                con.Open();
                tx = con.BeginTransaction();

                using (var cmd = new SqlCommand(
                    "DELETE FROM Submission WHERE doc_id = @id", con, tx))
                {
                    cmd.Parameters.AddWithValue("@id", docId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = new SqlCommand(
                    "DELETE FROM Documents WHERE doc_id = @id", con, tx))
                {
                    cmd.Parameters.AddWithValue("@id", docId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx?.Rollback();
                throw;
            }
            finally
            {
                con.Close();
            }
        }

        // Update Document by doc_id //
        public void UpdateDocument(int docId, string title, string content, int deptId)
        {
            try
            {
                con.Open();

                using var cmd = new SqlCommand(@"
            UPDATE Documents
            SET title = @title,
                content = @content,
                dept_id = @dept
            WHERE doc_id = @id", con);

                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@content", content ?? "");
                cmd.Parameters.AddWithValue("@dept", deptId);
                cmd.Parameters.AddWithValue("@id", docId);

                cmd.ExecuteNonQuery();
            }
            finally
            {
                con.Close();
            }
        }

        // --- Get all departments ---
        public DataTable GetAllDepartments()
        {
            DataTable dt = new DataTable();
            string query = "SELECT dept_id, specialization FROM Department ORDER BY specialization";

            try
            {
                con.Open();
                using SqlCommand cmd = new SqlCommand(query, con);
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        // --- Get all employees ---
        public DataTable GetAllEmployees()
        {
            DataTable dt = new DataTable();
            string query = "SELECT ssn, Fname + ' ' + Lname AS EmployeeName FROM Employee ORDER BY Fname, Lname";

            try
            {
                con.Open();
                using SqlCommand cmd = new SqlCommand(query, con);
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }


		/* ----------------- For Client -------------------*/

		public DataRow GetClientBySSN(string ssn)
		{
			DataTable dt = new DataTable();
			string query = "SELECT * FROM Client WHERE ssn = @Ssn";
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					dt.Load(cmd.ExecuteReader());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return dt.Rows.Count > 0 ? dt.Rows[0] : null;
		}

		public bool UpdateClient(string ssn, string name, string address, string email, string phoneNumber)
		{
			string query = @"
        UPDATE Client
        SET name = @Name,
            address = @Address,
            email = @Email,
            phone_number = @PhoneNumber
        WHERE ssn = @Ssn";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.Parameters.AddWithValue("@Name", (object)name ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Address", (object)address ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);

					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}
		public DataTable GetAllClients()
		{
			DataTable dt = new DataTable();
			string query = "SELECT ssn, name, email, phone_number, address FROM Client ORDER BY name";
			try
			{
				con.Open();
				using SqlCommand cmd = new SqlCommand(query, con);
				dt.Load(cmd.ExecuteReader());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return dt;
		}

		public DataTable SearchClient(string searchTerm)
		{
			DataTable dt = new DataTable();
			string query = @"
        SELECT ssn, name, email, phone_number, address 
        FROM Client 
        WHERE ssn LIKE @term OR name LIKE @term OR email LIKE @term
        ORDER BY name";
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@term", "%" + searchTerm + "%");
					dt.Load(cmd.ExecuteReader());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return dt;
		}

		public void DeleteClient(string ssn)
		{
			try
			{
				con.Open();
				string query = "DELETE FROM Client WHERE ssn = @Ssn";
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
		}

		public bool AddClient(string ssn, string name, string address, string email, string phoneNumber)
		{
			string query = @"
        INSERT INTO Client (ssn, name, address, email, phone_number)
        VALUES (@Ssn, @Name, @Address, @Email, @PhoneNumber)";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.Parameters.AddWithValue("@Name", (object)name ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Address", (object)address ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);

					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}
		// ----------------------------------For Dashboard---------------------

		public int GetTotalClients()
		{
			int count = 0;
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Client", con))
				{
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public int GetTotalEmployees()
		{
			return GetEmployeeCount(); // You already have this method
		}

		public int GetOpenProjects()
		{
			int count = 0;
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Project WHERE status = 'Open'", con))
				{
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public int GetPendingTasks()
		{
			int count = 0;
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Assigns WHERE status = 'Pending'", con))
				{
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public int GetUnreadEmails()
		{
			int count = 0;
			try
			{
				con.Open();
				// Corrected to use 'status' column matching the rest of the app
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM E_Case_Source WHERE status = 'Unread' OR status IS NULL", con))
				{
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public int GetOfficeRequestsCount()
		{
			int count = 0;
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Office_Requests", con))
				{
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public int GetOfficeRequestsForDepartment(string deptId)
		{
			int count = 0;
			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand("SELECT COUNT(*) FROM Office_Requests WHERE dept_id = @DeptId", con))
				{
					cmd.Parameters.AddWithValue("@DeptId", deptId);
					count = (int)cmd.ExecuteScalar();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return count;
		}

		public DataTable GetRecentActivities(int top = 5)
		{
			DataTable dt = new DataTable();
			string query = @"
                SELECT TOP (@top) TimeAgo, Activity, [User]
                FROM (
                    -- New Projects
                    SELECT TOP (@top) open_date AS SortDate, 
                           'New project opened: ' + description AS Activity,
                           'System' AS [User],
                           FORMAT(open_date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM Project
                    ORDER BY open_date DESC
                    
                    UNION ALL
                    
                    -- New Task Assignments
                    SELECT TOP (@top) assignment_date AS SortDate,
                           'Task assigned: ' + t.description AS Activity,
                           'Manager' AS [User],
                           FORMAT(assignment_date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM Assigns a
                    JOIN Task t ON a.task_id = t.task_id
                    ORDER BY assignment_date DESC
                    
                    UNION ALL
                    
                    -- New Office Requests
                    SELECT TOP (@top) date AS SortDate,
                           'New client request: ' + subject AS Activity,
                           c.name AS [User],
                           FORMAT(date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM Office_Requests r
                    JOIN Client c ON r.client_id = c.ssn
                    ORDER BY date DESC

                    UNION ALL

                    -- New Emails
                    SELECT TOP (@top) date AS SortDate,
                           'Email received: ' + subject AS Activity,
                           sender_email AS [User],
                           FORMAT(date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM E_Case_Source
                    ORDER BY date DESC
                ) AS Activities
                ORDER BY SortDate DESC";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@top", top);
					dt.Load(cmd.ExecuteReader());
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
			finally
			{
				con.Close();
			}
			return dt;
		}


		// -------------------------- Office Requests --------------------------
		public bool AddOfficeRequest(string clientId, string subject, string body, int deptId)
		{
			string query = @"
        INSERT INTO Office_Requests (client_id, date, subject, body, dept_id)
        VALUES (@ClientId, GETDATE(), @Subject, @Body, @DeptId)";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@ClientId", clientId);
					cmd.Parameters.AddWithValue("@Subject", subject);
					cmd.Parameters.AddWithValue("@Body", body);
					cmd.Parameters.AddWithValue("@DeptId", deptId);
					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}
		// -----------------------Settings-------------------
		public bool UpdateEmployeeProfile(string ssn, string name, string email, string phoneNumber)
		{
			string query = @"
        UPDATE Employee
        SET Fname = @Fname, 
            Lname = @Lname,
            email = @Email,
            phone_number = @PhoneNumber
        WHERE ssn = @Ssn";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.Parameters.AddWithValue("@Fname", name.Split(' ')[0]); 
					cmd.Parameters.AddWithValue("@Lname", name.Split(' ').Length > 1 ? name.Split(' ')[1] : ""); // Last name
					cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
					cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);
					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}



		public bool UpdateEmployeePassword(string ssn, string newPassword)
		{
			string query = "UPDATE Employee SET password = @Password WHERE ssn = @Ssn";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.Parameters.AddWithValue("@Password", newPassword);
					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}





		public bool UpdateNotificationPreferences(string ssn, bool emailAlerts, bool systemAlerts)
		{
			string query = @"
        UPDATE Employee
        SET email_alerts = @EmailAlerts,
            system_alerts = @SystemAlerts
        WHERE ssn = @Ssn";

			try
			{
				con.Open();
				using (SqlCommand cmd = new SqlCommand(query, con))
				{
					cmd.Parameters.AddWithValue("@Ssn", ssn);
					cmd.Parameters.AddWithValue("@EmailAlerts", emailAlerts);
					cmd.Parameters.AddWithValue("@SystemAlerts", systemAlerts);
					return cmd.ExecuteNonQuery() > 0;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return false;
			}
			finally
			{
				con.Close();
			}
		}

        // ----------------- REPORTING & CHARTS (With Date Filters) ----------------- //

        private (DateTime, DateTime) GetDateRange(DateTime? start, DateTime? end)
        {
            // Default: Last 30 days if no date selected
            DateTime s = start ?? DateTime.Today.AddDays(-30);
            DateTime e = (end ?? DateTime.Today).AddDays(1); // Always add 1 day to make the end date inclusive
            return (s, e);
        }
        public DataTable GetTaskStatusStats(DateTime? start, DateTime? end, int? deptId = null, string empSSN = null)
        {
            var (s, e) = GetDateRange(start, end);
            string query = @"
SELECT status, COUNT(*) as Count 
FROM Assigns 
WHERE assignment_date >= @start AND assignment_date < @end";

            if (deptId.HasValue) query += " AND dept_id = @deptId";
            if (!string.IsNullOrEmpty(empSSN)) query += " AND e_ssn = @empSSN";

            query += " GROUP BY status";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@start", s);
            cmd.Parameters.AddWithValue("@end", e);
            if (deptId.HasValue) cmd.Parameters.AddWithValue("@deptId", deptId.Value);
            if (!string.IsNullOrEmpty(empSSN)) cmd.Parameters.AddWithValue("@empSSN", empSSN);

            DataTable dt = new DataTable();
            try { con.Open(); dt.Load(cmd.ExecuteReader()); }
            finally { con.Close(); }
            return dt;
        }
        public DataTable GetTasksPerDept(DateTime? start, DateTime? end)
        {
            var (s, e) = GetDateRange(start, end);
            string query = @"
        SELECT d.specialization, COUNT(a.task_id) as Count
        FROM Department d
        LEFT JOIN Assigns a ON d.dept_id = a.dept_id 
             AND a.assignment_date >= @start AND a.assignment_date < @end
        GROUP BY d.specialization";

            return ExecuteDateQuery(query, s, e);
        }
        public DataTable GetRequestTimeline(DateTime? start, DateTime? end, int? deptId = null, string empSSN = null)
        {
            var (s, e) = GetDateRange(start, end);
            string sanitizedSSN = empSSN?.Trim();
            bool isFiltered = deptId.HasValue || !string.IsNullOrEmpty(sanitizedSSN);

            // Fetch Email for strict filtering if SSN is provided
            string empEmail = null;
            if (!string.IsNullOrEmpty(sanitizedSSN))
            {
                empEmail = GetEmailBySSN(sanitizedSSN);
            }

            string query = @"
SELECT 
    FORMAT(DateVal, 'yyyy-MM-dd') as DateLabel,
    ISNULL(SUM(EmailCount), 0) as Emails,
    ISNULL(SUM(OfficeCount), 0) as OfficeRequests
FROM (
    SELECT e.date as DateVal, 1 as EmailCount, 0 as OfficeCount 
    FROM E_Case_Source e
    LEFT JOIN Filter f ON e.email_id = f.email_id
    WHERE e.date >= @start AND e.date < @end
    AND (
        (@isFiltered = 0)
        
        -- Employee View: Match specific recipient
        OR (@empEmail IS NOT NULL AND e.recipient_email = @empEmail)
        
        -- Dept View (No SSN): Match Project
        OR (@empEmail IS NULL AND f.case_id IN (
            SELECT p2.Project_id FROM Project p2
            WHERE (@deptId IS NULL OR p2.dept_id = @deptId)
        ))
    )

    UNION ALL

    SELECT r.date as DateVal, 0 as EmailCount, 1 as OfficeCount 
    FROM Office_Requests r
    LEFT JOIN Filter f ON r.request_id = f.req_id
    WHERE r.date >= @start AND r.date < @end
    AND (
        (@isFiltered = 0)
        
        -- Office Requests likely do not have recipient_email, revert to Project logic or disable for this view?
        -- For now, falling back to Project logic even for employees if we can't link by email
        OR f.case_id IN (
            SELECT p3.Project_id FROM Project p3
            WHERE (@deptId IS NULL OR p3.dept_id = @deptId)
        )
    )
) as Combined
GROUP BY FORMAT(DateVal, 'yyyy-MM-dd')
ORDER BY DateLabel";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@start", s);
            cmd.Parameters.AddWithValue("@end", e);
            cmd.Parameters.AddWithValue("@deptId", (object)deptId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@empEmail", (object)empEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isFiltered", isFiltered ? 1 : 0);

            DataTable dt = new DataTable();
            try { con.Open(); dt.Load(cmd.ExecuteReader()); }
            finally { con.Close(); }
            return dt;
        }

        public DataTable GetEmailBreakdown(DateTime? start, DateTime? end, int? deptId = null, string empSSN = null)
        {
            var (s, e) = GetDateRange(start, end);
            string sanitizedSSN = empSSN?.Trim();
            bool isFiltered = deptId.HasValue || !string.IsNullOrEmpty(sanitizedSSN);
            
            // Fetch Email for strict filtering if SSN is provided
            string empEmail = null;
            if (!string.IsNullOrEmpty(sanitizedSSN))
            {
                empEmail = GetEmailBySSN(sanitizedSSN);
            }

            // NEW QUERY: Combine emails AND office requests
            string query = @"
        SELECT 
            SUM(CASE WHEN f.ref IS NOT NULL AND @isFiltered = 0 THEN 1 ELSE 0 END) as Spam,

            -- Assigned Logic (Read emails and requests):
            SUM(CASE 
                -- Global View: Read emails with projects
                WHEN @isFiltered = 0 AND vp.Project_id IS NOT NULL AND e.status = 'Read' THEN 1
                
                -- Employee View: Their read emails
                WHEN @empEmail IS NOT NULL AND e.recipient_email = @empEmail 
                     AND e.status = 'Read' THEN 1

                -- Dept View: Dept read emails
                WHEN @empEmail IS NULL AND @deptId IS NOT NULL AND vp.Project_id IS NOT NULL 
                     AND e.status = 'Read' THEN 1
                ELSE 0 
            END) 
            +
            -- Add read office requests
            (SELECT COUNT(*) FROM Office_Requests r
             WHERE r.date >= @start AND r.date < @end
             AND r.status = 'Read'
             AND (@deptId IS NULL OR r.dept_id = @deptId))
            as Assigned,

            -- NotRead Logic (Unread emails + Unread requests):
            SUM(CASE 
                -- Global View: Unread emails (not spam, not assigned)
                WHEN @isFiltered = 0 AND (e.status = 'Unread' OR e.status IS NULL) 
                     AND f.ref IS NULL AND f.case_id IS NULL THEN 1
                
                -- Employee View: Their unread emails
                WHEN @empEmail IS NOT NULL AND e.recipient_email = @empEmail 
                     AND (e.status = 'Unread' OR e.status IS NULL) THEN 1

                -- Dept View: Dept unread emails
                WHEN @empEmail IS NULL AND @deptId IS NOT NULL AND vp.Project_id IS NOT NULL 
                     AND (e.status = 'Unread' OR e.status IS NULL) THEN 1
                ELSE 0 
            END)
            +
            -- Add unread office requests
            (SELECT COUNT(*) FROM Office_Requests r
             WHERE r.date >= @start AND r.date < @end
             AND (r.status = 'Unread' OR r.status IS NULL)
             AND (@deptId IS NULL OR r.dept_id = @deptId))
            as NotRead

        FROM E_Case_Source e
        LEFT JOIN Filter f ON e.email_id = f.email_id
        LEFT JOIN (
            SELECT DISTINCT p.Project_id
            FROM Project p
            LEFT JOIN Assigns a ON p.Project_id = a.task_id
            WHERE (@deptId IS NULL OR p.dept_id = @deptId)
        ) vp ON f.case_id = vp.Project_id
        WHERE e.date >= @start AND e.date < @end
        AND (
            @isFiltered = 0
            OR (@empEmail IS NOT NULL AND e.recipient_email = @empEmail)
            OR (@empEmail IS NULL AND vp.Project_id IS NOT NULL)
        )";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@start", s);
            cmd.Parameters.AddWithValue("@end", e);
            cmd.Parameters.AddWithValue("@deptId", (object)deptId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@empEmail", (object)empEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isFiltered", isFiltered ? 1 : 0);

            DataTable dt = new DataTable();
            try { con.Open(); dt.Load(cmd.ExecuteReader()); }
            finally { con.Close(); }
            return dt;
        }

        private string GetEmailBySSN(string ssn)
        {
             string q = "SELECT email FROM Employee WHERE ssn = @ssn";
             if (con.State == ConnectionState.Open) con.Close();
             using SqlCommand cmd = new SqlCommand(q, con);
             cmd.Parameters.AddWithValue("@ssn", ssn);
             con.Open();
             object res = cmd.ExecuteScalar();
             con.Close();
             return res?.ToString();
        }
        private DataTable ExecuteDateQuery(string query, DateTime start, DateTime end)
        {
            DataTable dt = new DataTable();
            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
            finally { con.Close(); }
            return dt;
        }
        public DataTable GetTasks()
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM vw_All_Assigned_Tasks";

            using SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

            }
            finally
            {
                con.Close();
            }

            return dt;
        }

        ////////////////////////////////////////////////////////////////////////////

        public DataTable SearchTasks(string keyword)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT *
                FROM vw_All_Assigned_Tasks
                WHERE CAST(task_id AS NVARCHAR) LIKE @key
                   OR description LIKE @key
                   OR assigning_department_name LIKE @key
                   OR task_status LIKE @key";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@key", "%" + keyword + "%");

            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

            }
            finally
            {
                con.Close();
            }

            return dt;
        }


        public void AddTask(
            string description,
            int deptId,
            string empSSN,
            DateTime assignmentDate,
            DateTime deadline)
        {
            SqlTransaction transaction = null;

            try
            {
                con.Open();
                transaction = con.BeginTransaction(System.Data.IsolationLevel.Serializable);

                // 1. Generate new ID
                int newTaskId;
                string getNewId = "SELECT ISNULL(MAX(task_id), 0) + 1 FROM Task WITH (UPDLOCK, HOLDLOCK)";
                using (SqlCommand cmd = new SqlCommand(getNewId, con, transaction))
                {
                    newTaskId = (int)cmd.ExecuteScalar();
                }

                // 2. Insert Task
                string queryTask = "INSERT INTO Task (task_id, description) VALUES (@task_id, @desc)";
                using (SqlCommand cmd = new SqlCommand(queryTask, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@task_id", newTaskId);
                    cmd.Parameters.AddWithValue("@desc", description);
                    cmd.ExecuteNonQuery();
                }

                // 3. Insert Assignment
                string queryAssign = @"INSERT INTO Assigns (dept_id, task_id, e_ssn, assignment_date, deadline, status)
                                     VALUES (@dept_id, @task_id, @emp_ssn, @assign_date, @deadline, 'Pending')";
                
                using (SqlCommand cmd = new SqlCommand(queryAssign, con, transaction))
                {
                    cmd.Parameters.AddWithValue("@dept_id", deptId);
                    cmd.Parameters.AddWithValue("@task_id", newTaskId);
                    cmd.Parameters.AddWithValue("@emp_ssn", empSSN);
                    cmd.Parameters.AddWithValue("@assign_date", assignmentDate);
                    cmd.Parameters.AddWithValue("@deadline", deadline);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction?.Rollback();
                Console.WriteLine(ex);
            }
            finally
            {
                con.Close();
            }
        }


        public void DeleteTask(int taskId)
        {
            string query = @"
                DELETE FROM Assigns WHERE task_id = @id;
                DELETE FROM Task WHERE task_id = @id;
            ";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", taskId);

            try
            {
                con.Open();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

            }
            finally
            {
                con.Close();
            }
        }


        public DataTable GetDepartments()
        {
            DataTable dt = new DataTable();
            string query = "SELECT dept_id, specialization FROM Department";

            using SqlCommand cmd = new SqlCommand(query, con);

            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);

            }
            finally
            {
                con.Close();
            }

            return dt;
        }


        //public DataTable GetEmployeesByDepartment(int deptId)
        //{
        //    DataTable dt = new DataTable();
        //    string query = @"
        //        SELECT ssn, CONCAT(Fname, ' ', Lname) AS full_name
        //        FROM Employee
        //        WHERE dept_id = @dept";

        //    using SqlCommand cmd = new SqlCommand(query, con);
        //    cmd.Parameters.AddWithValue("@dept", deptId);

        //    try
        //    {
        //        con.Open();
        //        dt.Load(cmd.ExecuteReader());
        //    }
        //    catch (Exception ex)
        //    {

        //        Console.WriteLine(ex);

        //    }
        //    finally
        //    {
        //        con.Close();
        //    }

        //    return dt;
        //}
        public DataRow GetTaskById(int taskId)
        {
            DataTable dt = new DataTable();

            string query = @"
        SELECT t.task_id, t.description, a.dept_id, a.e_ssn,
               a.assignment_date, a.deadline
        FROM Task t
        JOIN Assigns a ON t.task_id = a.task_id
        WHERE t.task_id = @id";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", taskId);

            con.Open();
            dt.Load(cmd.ExecuteReader());
            con.Close();
            if (dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }

        public void UpdateTask(int taskId, string desc, int deptId, string empSSN,
                               DateTime assignDate, DateTime deadline)
        {
            string query = @"
        UPDATE Task
        SET description = @desc
        WHERE task_id = @id;

        UPDATE Assigns
        SET dept_id = @dept,
            e_ssn = @emp,
            assignment_date = @assign,
            deadline = @deadline
        WHERE task_id = @id;
    ";

            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", taskId);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.Parameters.AddWithValue("@dept", deptId);
            cmd.Parameters.AddWithValue("@emp", empSSN);
            cmd.Parameters.AddWithValue("@assign", assignDate);
            cmd.Parameters.AddWithValue("@deadline", deadline);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }
        public DataTable GetUnifiedInboundCommunications(string role, string userSsn, string userDeptId, string userEmail)
        {
            DataTable dt = new DataTable();
            string query = "";

            if (role == "admin")
            {
                query = @"
                    SELECT email_id AS Id, sender_email AS Sender, subject AS Subject, body AS Body, date AS Date, ISNULL(status,'Unread') AS Status, 'Email' AS Type, NULL AS DeptId
                    FROM E_Case_Source
                    UNION ALL
                    SELECT r.request_id, c.name, r.subject, r.body, r.date, ISNULL(r.status, 'Unread'), 'Request', r.dept_id
                    FROM Office_Requests r
                    JOIN Client c ON r.client_id = c.ssn
                    ORDER BY Date DESC";
            }
            else if (role == "manager")
            {
                // Managers see emails sent to them OR requests for their department
                query = @"
                    SELECT email_id AS Id, sender_email AS Sender, subject AS Subject, body AS Body, date AS Date, ISNULL(status,'Unread') AS Status, 'Email' AS Type, NULL AS DeptId
                    FROM E_Case_Source
                    WHERE recipient_email = @userEmail
                    UNION ALL
                    SELECT r.request_id, c.name, r.subject, r.body, r.date, ISNULL(r.status, 'Unread'), 'Request', r.dept_id
                    FROM Office_Requests r
                    JOIN Client c ON r.client_id = c.ssn
                    WHERE r.dept_id = @userDeptId
                    ORDER BY Date DESC";
            }
            else if (role == "employee")
            {
                // Employees see only their emails
                query = @"
                    SELECT email_id AS Id, sender_email AS Sender, subject AS Subject, body AS Body, date AS Date, ISNULL(status,'Unread') AS Status, 'Email' AS Type, NULL AS DeptId
                    FROM E_Case_Source
                    WHERE recipient_email = @userEmail
                    ORDER BY Date DESC";
            }

            if (string.IsNullOrEmpty(query)) return dt;

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@userEmail", userEmail ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@userDeptId", userDeptId ?? (object)DBNull.Value);

            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            finally
            {
                con.Close();
            }

            return dt;
        }

        public DataTable GetEmails()
        {
            DataTable dt = new DataTable();
            string query = "SELECT email_id, sender_email, subject, body, date, ISNULL(status,'Unread') AS status FROM E_Case_Source ORDER BY date DESC";

            if (con.State == ConnectionState.Open)
                con.Close();

            using SqlCommand cmd = new SqlCommand(query, con);
            con.Open();
            dt.Load(cmd.ExecuteReader());
            con.Close();

            return dt;
        }

        public DataRow GetEmailById(int emailId)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM E_Case_Source WHERE email_id = @id";

            if (con.State == ConnectionState.Open)
                con.Close();

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", emailId);

            con.Open();
            dt.Load(cmd.ExecuteReader());
            con.Close();

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0];
        }

        public DataRow GetRequestById(int requestId)
        {
            DataTable dt = new DataTable();
            string query = "SELECT * FROM Office_Requests WHERE request_id = @id";

            if (con.State == ConnectionState.Open)
                con.Close();

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", requestId);

            con.Open();
            dt.Load(cmd.ExecuteReader());
            con.Close();

            if (dt.Rows.Count == 0)
                return null;

            return dt.Rows[0];
        }

        public void MarkRequestAsRead(int requestId)
        {
            if (con.State == ConnectionState.Open) con.Close();
            string q = "UPDATE Office_Requests SET status = 'Read' WHERE request_id = @id";
            using SqlCommand cmd = new SqlCommand(q, con);
            cmd.Parameters.AddWithValue("@id", requestId);
            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void AttachEmailToTask(int taskId, string emailBody)
        {
            string query = @"
        UPDATE Task
        SET description = description + CHAR(10) + CHAR(10)
            + '--- Attached Email ---' + CHAR(10)
            + @emailBody
        WHERE task_id = @taskId";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@taskId", taskId);
            cmd.Parameters.AddWithValue("@emailBody", emailBody);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public DataRow TryGetTaskById(int id)
        {
            DataTable dt = new DataTable();

            string q = @"
        SELECT t.task_id, t.description, a.dept_id, a.e_ssn,
               a.assignment_date, a.deadline
        FROM Task t
        JOIN Assigns a ON t.task_id = a.task_id
        WHERE t.task_id = @id";

            using SqlCommand cmd = new SqlCommand(q, con);
            cmd.Parameters.AddWithValue("@id", id);

            con.Open();
            dt.Load(cmd.ExecuteReader());
            con.Close();
            if (dt.Rows.Count == 0)
                return null;
            return dt.Rows[0];
        }
        public int GenerateNewTaskId()
        {
            SqlCommand cmd = new SqlCommand("SELECT ISNULL(MAX(task_id),0)+1 FROM Task", con);
            con.Open();
            int id = (int)cmd.ExecuteScalar();
            con.Close();
            return id;
        }
        public void MarkEmailAsRead(int emailId)
        {
            if (con.State == ConnectionState.Open)
                con.Close();

            string query = "UPDATE E_Case_Source SET status = 'Read' WHERE email_id = @id";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", emailId);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void MarkEmailAsUnread(int emailId)
        {
            if (con.State == ConnectionState.Open)
                con.Close();

            string query = "UPDATE E_Case_Source SET status = 'Unread' WHERE email_id = @id";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", emailId);

            con.Open();
            cmd.ExecuteNonQuery();
            con.Close();
        }


        // --- Role-Based Filtered Document Search ---
        public DataTable GetFilteredDocuments(string role, string userSsn, string userDeptId, int? filterDeptId, string filterEmpSsn, string keyword)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT d.*, s.emp_ssn, s.submission_date
                FROM Documents d
                JOIN Submission s ON d.doc_id = s.doc_id
                WHERE (1=1)
            ";

            // Role-Based Mandatory Filters
            if (role == "manager")
            {
                query += " AND d.dept_id = @UserDeptId ";
            }
            else if (role == "employee")
            {
                query += " AND s.emp_ssn = @UserSsn ";
            }

            // Optional User Filters
            if (filterDeptId.HasValue && filterDeptId > 0)
            {
                query += " AND d.dept_id = @FilterDeptId ";
            }
            if (!string.IsNullOrEmpty(filterEmpSsn))
            {
                query += " AND s.emp_ssn = @FilterEmpSsn ";
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                query += " AND (d.title LIKE @kw OR d.content LIKE @kw OR CAST(d.doc_id AS NVARCHAR) LIKE @kw) ";
            }

            query += " ORDER BY s.submission_date DESC";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                if (role == "manager") cmd.Parameters.AddWithValue("@UserDeptId", userDeptId);
                if (role == "employee") cmd.Parameters.AddWithValue("@UserSsn", userSsn);
                
                if (filterDeptId.HasValue && filterDeptId > 0) cmd.Parameters.AddWithValue("@FilterDeptId", filterDeptId.Value);
                if (!string.IsNullOrEmpty(filterEmpSsn)) cmd.Parameters.AddWithValue("@FilterEmpSsn", filterEmpSsn);
                if (!string.IsNullOrEmpty(keyword)) cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");

                try
                {
                    con.Open();
                    dt.Load(cmd.ExecuteReader());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    con.Close();
                }
            }

            return dt;
        }

        public DataTable GetEmployeesByDept(int deptId)
        {
            DataTable dt = new DataTable();
            // FILTER LOGIC: Only show employees in this Dept who have data in Assigns OR Filter (emails)
            string query = @"
                SELECT DISTINCT e.ssn, e.Fname + ' ' + e.Lname AS EmployeeName 
                FROM Employee e
                LEFT JOIN Assigns a ON e.ssn = a.e_ssn
                LEFT JOIN E_Case_Source mail ON e.email = mail.recipient_email
                WHERE e.dept_id = @dept 
                  AND (a.task_id IS NOT NULL OR mail.email_id IS NOT NULL)
                ORDER BY EmployeeName";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@dept", deptId);
            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        public DataTable GetEmployeesWithDocuments(int? deptId)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT DISTINCT e.ssn, e.Fname + ' ' + e.Lname AS EmployeeName 
                FROM Employee e
                JOIN Submission s ON e.ssn = s.emp_ssn
                WHERE (1=1)
            ";
            
            if (deptId.HasValue && deptId > 0)
            {
                query += " AND e.dept_id = @dept";
            }
            
            query += " ORDER BY EmployeeName";

            using SqlCommand cmd = new SqlCommand(query, con);
            if (deptId.HasValue && deptId > 0) cmd.Parameters.AddWithValue("@dept", deptId);

            try
            {
                con.Open();
                dt.Load(cmd.ExecuteReader());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        // ----------------- MANAGER DASHBOARD METHODS ----------------- //

        public int GetTotalClientsForDepartment(string deptId)
        {
            int count = 0;
            try
            {
                con.Open();
                // Count clients with projects in this department
                string query = @"
            SELECT COUNT(DISTINCT c.ssn) 
            FROM Client c
            INNER JOIN Project p ON c.ssn = p.client_id
            WHERE p.dept_id = @DeptId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    count = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return count;
        }

        public int GetTotalEmployeesForDepartment(string deptId)
        {
            int count = 0;
            try
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Employee WHERE dept_id = @DeptId";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    count = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return count;
        }

        public int GetOpenProjectsForDepartment(string deptId)
        {
            int count = 0;
            try
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Project WHERE dept_id = @DeptId AND status = 'Open'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    count = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return count;
        }

        public int GetPendingTasksForDepartment(string deptId)
        {
            int count = 0;
            try
            {
                con.Open();
                string query = "SELECT COUNT(*) FROM Assigns WHERE dept_id = @DeptId AND status = 'Pending'";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    count = (int)cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return count;
        }


        public DataTable GetRecentActivitiesForDepartment(string deptId, int top = 5)
        {
            DataTable dt = new DataTable();
            string query = @"
                SELECT TOP (@top) TimeAgo, Activity, [User]
                FROM (
                    -- Dept Projects
                    SELECT TOP (@top) open_date AS SortDate, 
                           'New project: ' + description AS Activity,
                           'System' AS [User],
                           FORMAT(open_date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM Project
                    WHERE dept_id = @DeptId
                    ORDER BY open_date DESC
                    
                    UNION ALL
                    
                    -- Dept Tasks
                    SELECT TOP (@top) assignment_date AS SortDate,
                           'Task assigned: ' + t.description AS Activity,
                           'Manager' AS [User],
                           FORMAT(assignment_date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM Assigns a
                    JOIN Task t ON a.task_id = t.task_id
                    WHERE a.dept_id = @DeptId
                    ORDER BY assignment_date DESC

                    UNION ALL

                    -- Emails (Visible to all managers)
                    SELECT TOP (@top) date AS SortDate,
                           'Email received: ' + subject AS Activity,
                           sender_email AS [User],
                           FORMAT(date, 'MMM dd, HH:mm') AS TimeAgo
                    FROM E_Case_Source
                    ORDER BY date DESC
                ) AS Activities
                ORDER BY SortDate DESC";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@top", top);
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        //public DataTable SearchEmployee(string searchTerm)
        //{
        //    DataTable dt = new DataTable();
        //    if (string.IsNullOrWhiteSpace(searchTerm))
        //    {
        //        return dt;
        //    }

        //    string query = @"
        //SELECT * FROM Employee 
        //WHERE ssn LIKE @term 
        //   OR Fname LIKE @term 
        //   OR Lname LIKE @term 
        //   OR CONCAT(Fname, ' ', Lname) LIKE @term
        //   OR email LIKE @term";

        //    try
        //    {
        //        con.Open();
        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@term", "%" + searchTerm + "%");
        //            dt.Load(cmd.ExecuteReader());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //    finally
        //    {
        //        con.Close();
        //    }
        //    return dt;
        //}

        // Search employees within a specific department (for Manager)
        //public DataTable SearchEmployeeInDepartment(string searchTerm, int deptId)
        //{
        //    DataTable dt = new DataTable();
        //    if (string.IsNullOrWhiteSpace(searchTerm))
        //    {
        //        return dt;
        //    }

        //    string query = @"
        //SELECT * FROM Employee 
        //WHERE dept_id = @DeptId 
        //  AND (ssn LIKE @term 
        //       OR Fname LIKE @term 
        //       OR Lname LIKE @term 
        //       OR CONCAT(Fname, ' ', Lname) LIKE @term
        //       OR email LIKE @term)";

        //    try
        //    {
        //        con.Open();
        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@DeptId", deptId);
        //            cmd.Parameters.AddWithValue("@term", "%" + searchTerm + "%");
        //            dt.Load(cmd.ExecuteReader());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //    finally
        //    {
        //        con.Close();
        //    }
        //    return dt;
        //}

        // Get all employees by department (for Manager)
        //public DataTable GetEmployeesByDepartment(int deptId)
        //{
        //    DataTable dt = new DataTable();
        //    string query = "SELECT * FROM Employee WHERE dept_id = @DeptId ORDER BY Fname, Lname";

        //    try
        //    {
        //        con.Open();
        //        using (SqlCommand cmd = new SqlCommand(query, con))
        //        {
        //            cmd.Parameters.AddWithValue("@DeptId", deptId);
        //            dt.Load(cmd.ExecuteReader());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }
        //    finally
        //    {
        //        con.Close();
        //    }
        //    return dt;
        //}

        public DataTable GetClientsForDepartment(int deptId)
        {
            DataTable dt = new DataTable();
            string query = @"
        SELECT DISTINCT c.ssn, c.name, c.email, c.phone_number, c.address
        FROM Client c
        INNER JOIN Project p ON c.ssn = p.client_id
        WHERE p.dept_id = @DeptId
        ORDER BY c.name";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }

        // Search clients within a specific department
        public DataTable SearchClientInDepartment(string searchTerm, int deptId)
        {
            DataTable dt = new DataTable();
            string query = @"
        SELECT DISTINCT c.ssn, c.name, c.email, c.phone_number, c.address 
        FROM Client c
        INNER JOIN Project p ON c.ssn = p.client_id
        WHERE p.dept_id = @DeptId
          AND (c.ssn LIKE @term OR c.name LIKE @term OR c.email LIKE @term)
        ORDER BY c.name";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@DeptId", deptId);
                    cmd.Parameters.AddWithValue("@term", "%" + searchTerm + "%");
                    dt.Load(cmd.ExecuteReader());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                con.Close();
            }
            return dt;
        }


        // ========================== Company Logo ==========================
        
        public (byte[] LogoData, string ContentType, bool Success) GetCompanyLogo()
        {
            byte[] logoData = null;
            string contentType = "image/png";
            bool success = false;

            string query = "SELECT logo_image, content_type FROM dbo.GetCompanyLogo()";

            try
            {
                con.Open();
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                logoData = (byte[])reader["logo_image"];
                                if (!reader.IsDBNull(1))
                                {
                                    contentType = reader["content_type"].ToString();
                                }
                                success = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving logo: {ex.Message}");
            }
            finally
            {
                con.Close();
            }

            return (logoData, contentType, success);
        }

    // ========================== Two-Factor Authentication (2FA) ==========================

    /// <summary>
    /// Generates a random 6-digit OTP code
    /// </summary>
    public string GenerateOTP()
    {
        Random random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Creates an OTP record in the database with expiration time
    /// </summary>
    public bool CreateOTPRecord(string userEmail, string otpCode, string purpose)
    {
        try
        {
            // Clean up any existing unused OTPs for this user and purpose
            CleanupUserOTPs(userEmail, purpose);

            string query = @"
                INSERT INTO OTPVerification (user_email, otp_code, purpose, created_at, expires_at, is_used, attempts)
                VALUES (@email, @code, @purpose, GETUTCDATE(), DATEADD(MINUTE, 5, GETUTCDATE()), 0, 0)";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@email", userEmail);
                cmd.Parameters.AddWithValue("@code", otpCode);
                cmd.Parameters.AddWithValue("@purpose", purpose);

                if (con.State != ConnectionState.Open) con.Open();
                cmd.ExecuteNonQuery();
                con.Close();
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating OTP record: {ex.Message}");
            if (con.State == ConnectionState.Open) con.Close();
            return false;
        }
    }

    /// <summary>
    /// Validates an OTP code for a specific user and purpose
    /// </summary>
    public (bool IsValid, string Message) ValidateOTP(string userEmail, string otpCode, string purpose)
    {
        try
        {
            string query = @"
                SELECT otp_id, expires_at, is_used, attempts
                FROM OTPVerification
                WHERE user_email = @email 
                  AND otp_code = @code 
                  AND purpose = @purpose
                  AND is_used = 0
                ORDER BY created_at DESC";

            if (con.State != ConnectionState.Open) con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@email", userEmail);
                cmd.Parameters.AddWithValue("@code", otpCode);
                cmd.Parameters.AddWithValue("@purpose", purpose);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int otpId = (int)reader["otp_id"];
                        DateTime expiresAt = (DateTime)reader["expires_at"];
                        int attempts = (int)reader["attempts"];

                        reader.Close();

                        // Check if OTP has expired (Compare with UTC NOW)
                        if (DateTime.UtcNow > expiresAt)
                        {
                            con.Close();
                            return (false, "OTP has expired. Please request a new one.");
                        }

                        // Check max attempts
                        if (attempts >= 3)
                        {
                            con.Close();
                            return (false, "Maximum attempts exceeded. Please request a new OTP.");
                        }

                        // Mark OTP as used
                        MarkOTPAsUsed(otpId);
                        con.Close();
                        return (true, "OTP verified successfully.");
                    }
                    else
                    {
                        reader.Close();
                        
                        // Increment attempts for any matching OTP
                        IncrementOTPAttempts(userEmail, otpCode, purpose);
                        
                        con.Close();
                        return (false, "Invalid OTP code.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating OTP: {ex.Message}");
            if (con.State == ConnectionState.Open) con.Close();
            return (false, "An error occurred while validating OTP.");
        }
    }

    /// <summary>
    /// Marks an OTP as used
    /// </summary>
    private void MarkOTPAsUsed(int otpId)
    {
        try
        {
            string query = "UPDATE OTPVerification SET is_used = 1 WHERE otp_id = @id";
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@id", otpId);
                
                if (con.State != ConnectionState.Open) con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error marking OTP as used: {ex.Message}");
        }
    }

    /// <summary>
    /// Increments the attempt counter for an OTP
    /// </summary>
    private void IncrementOTPAttempts(string userEmail, string otpCode, string purpose)
    {
        try
        {
            string query = @"
                UPDATE OTPVerification 
                SET attempts = attempts + 1 
                WHERE user_email = @email 
                  AND otp_code = @code 
                  AND purpose = @purpose 
                  AND is_used = 0";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@email", userEmail);
                cmd.Parameters.AddWithValue("@code", otpCode);
                cmd.Parameters.AddWithValue("@purpose", purpose);

                if (con.State != ConnectionState.Open) con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error incrementing OTP attempts: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleans up expired or unused OTPs for a specific user and purpose
    /// </summary>
    private void CleanupUserOTPs(string userEmail, string purpose)
    {
        try
        {
            string query = @"
                DELETE FROM OTPVerification 
                WHERE user_email = @email 
                  AND purpose = @purpose 
                  AND (is_used = 1 OR expires_at < GETUTCDATE())";

            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@email", userEmail);
                cmd.Parameters.AddWithValue("@purpose", purpose);

                if (con.State != ConnectionState.Open) con.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up OTPs: {ex.Message}");
        }
        finally
        {
            if (con.State == ConnectionState.Open) con.Close();
        }
    }

    /// <summary>
    /// Cleans up all expired OTP records (can be called periodically)
    /// </summary>
    public void CleanupExpiredOTPs()
    {
        try
        {
            string query = "DELETE FROM OTPVerification WHERE expires_at < GETDATE() OR is_used = 1";
            
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                int deleted = cmd.ExecuteNonQuery();
                Console.WriteLine($"Cleaned up {deleted} expired/used OTP records");
            }
            con.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up expired OTPs: {ex.Message}");
            if (con.State == ConnectionState.Open) con.Close();
        }
    }

    /// <summary>
    /// Gets the 2FA preference for a user (whether they have 2FA enabled for login)
    /// </summary>
    public bool Get2FAPreference(string userEmail)
    {
        try
        {
            string query = "SELECT enable_2fa_login FROM Employee WHERE email = @email";
            
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@email", userEmail);
                
                object result = cmd.ExecuteScalar();
                con.Close();
                
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToBoolean(result);
                }
                
                return false; // Default to disabled
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting 2FA preference: {ex.Message}");
            if (con.State == ConnectionState.Open) con.Close();
            return false;
        }
    }

    /// <summary>
    /// Updates the 2FA preference for a user
    /// </summary>
    public bool Update2FAPreference(string userSsn, bool enable2FA)
    {
        try
        {
            string query = "UPDATE Employee SET enable_2fa_login = @enable WHERE ssn = @ssn";
            
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@enable", enable2FA);
                cmd.Parameters.AddWithValue("@ssn", userSsn);
                
                cmd.ExecuteNonQuery();
                con.Close();
                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating 2FA preference: {ex.Message}");
            if (con.State == ConnectionState.Open) con.Close();
            return false;
        }
    }
    public DataTable GetEmployeesByDepartment(int deptId)
    {
        DataTable dt = new DataTable();
        string query = "SELECT * FROM Employee WHERE dept_id = @DeptId";

        try
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@DeptId", deptId);
                dt.Load(cmd.ExecuteReader());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            con.Close();
        }
        return dt;
    }

    public DataTable SearchEmployee(string searchTerm)
    {
        DataTable dt = new DataTable();
        string query = @"SELECT * FROM Employee 
                         WHERE ssn LIKE @Search 
                         OR Fname LIKE @Search 
                         OR Lname LIKE @Search";

        try
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@Search", "%" + searchTerm + "%");
                dt.Load(cmd.ExecuteReader());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            con.Close();
        }
        return dt;
    }

    public DataTable SearchEmployeeInDepartment(string searchTerm, int deptId)
    {
        DataTable dt = new DataTable();
        string query = @"SELECT * FROM Employee 
                         WHERE dept_id = @DeptId 
                         AND (ssn LIKE @Search 
                              OR Fname LIKE @Search 
                              OR Lname LIKE @Search)";

        try
        {
            con.Open();
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@DeptId", deptId);
                cmd.Parameters.AddWithValue("@Search", "%" + searchTerm + "%");
                dt.Load(cmd.ExecuteReader());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            con.Close();
        }
        return dt;
    }
}
}
