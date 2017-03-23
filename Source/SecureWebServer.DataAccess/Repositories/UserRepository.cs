using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SecureWebServer.Core.Entities;

namespace SecureWebServer.DataAccess.Repositories
{
    /// <summary>
    /// Repository for the retrieving and storing <see cref="User"/> entities.
    /// </summary>
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["SecureWebServer"].ConnectionString;
        }

        /// <summary>
        /// Retrieves all users from the repository.
        /// </summary>
        /// <returns></returns>
        public IList<User> GetAll()
        {
            List<User> users = new List<User>();

            using (IDbConnection conn = OpenConnection())
            {
                using (IDbCommand cmd = CreateCommand(conn))
                {
                    cmd.CommandText = "SELECT Id, Name, PasswordHash, PasswordSalt, Roles FROM [User]";

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            User user = new User();
                            PopulateUser(reader, user);
                            users.Add(user);
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        public User GetById(Guid id)
        {
            User user = null;

            using (IDbConnection conn = OpenConnection())
            {
                using (IDbCommand cmd = CreateCommand(conn))
                {
                    cmd.CommandText = "SELECT Id, Name, PasswordHash, PasswordSalt, Roles FROM [User] WHERE Id = @Id";

                    // Using parameters prevents SQL injection
                    IDbDataParameter idParam = cmd.CreateParameter();
                    idParam.ParameterName = "Id";
                    idParam.Value = id;
                    cmd.Parameters.Add(idParam);

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User();
                            PopulateUser(reader, user);
                        }
                    }
                }
            }

            return user;
        }

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        public User GetByName(string username)
        {
            User user = null;

            using (IDbConnection conn = OpenConnection())
            {
                using (IDbCommand cmd = CreateCommand(conn))
                {
                    cmd.CommandText = "SELECT Id, Name, PasswordHash, PasswordSalt, Roles FROM [User] WHERE Name = @Username";

                    // Using parameters prevents SQL injection
                    IDbDataParameter usernameParam = cmd.CreateParameter();
                    usernameParam.ParameterName = "Username";
                    usernameParam.Value = username;
                    cmd.Parameters.Add(usernameParam);

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = new User();
                            PopulateUser(reader, user);
                        }
                    }
                }
            }

            return user;
        }

        /// <summary>
        /// Saves changes to an existing user.
        /// </summary>
        public void Update(User user)
        {
            // Currently, only the Roles column may be updated

            using (IDbConnection conn = OpenConnection())
            {
                using (IDbCommand cmd = CreateCommand(conn))
                {
                    cmd.CommandText = "UPDATE [User] SET Roles = @Roles WHERE Id = @Id";

                    // Using parameters prevents SQL injection

                    IDbDataParameter idParam = cmd.CreateParameter();
                    idParam.ParameterName = "Id";
                    idParam.Value = user.Id;
                    cmd.Parameters.Add(idParam);

                    IDbDataParameter rolesParam = cmd.CreateParameter();
                    rolesParam.ParameterName = "Roles";
                    rolesParam.Value = string.Join("|", user.Roles);
                    cmd.Parameters.Add(rolesParam);

                    cmd.ExecuteScalar();
                }
            }
        }

        #region Helper methods for lazy devs

        // Reads the data into an existing user object.
        private void PopulateUser(IDataReader reader, User user)
        {
            user.Id = (Guid)reader["Id"];
            user.Name = (string)reader["Name"];
            user.PasswordHash = (string)reader["PasswordHash"];
            user.PasswordSalt = (string)reader["PasswordSalt"];
            user.Roles = ((string)reader["Roles"]).Split('|');
        }

        private IDbConnection OpenConnection()
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private IDbCommand CreateCommand(IDbConnection conn)
        {
            IDbCommand cmd = conn.CreateCommand();
            cmd.Connection = conn;
            return cmd;
        }

        #endregion
    }
}