using System.Collections.Generic;
using System.Linq;
using MoneyFox.DataAccess.DatabaseModels;
using MoneyFox.Foundation.Constants;
using MoneyFox.Foundation.DataModels;
using MoneyFox.Foundation.Interfaces;
using MvvmCross.Plugins.File;
using MvvmCross.Plugins.Sqlite;
using SQLite;

namespace MoneyFox.DataAccess
{
    /// <summary>
    ///     Helps with create update and connecting to the database.
    /// </summary>
    public class DatabaseManager : IDatabaseManager
    {
        private readonly IMvxSqliteConnectionFactory connectionFactory;
        private readonly IMvxFileStore fileStore;

        /// <summary>
        ///     Creates a new Database manager object
        /// </summary>
        /// <param name="connectionFactory">The connection factory who creates the connection for each plattform.</param>
        /// <param name="fileStore">An FileStore abstraction to access the file system on each plattform.</param>
        public DatabaseManager(IMvxSqliteConnectionFactory connectionFactory, IMvxFileStore fileStore)
        {
            this.connectionFactory = connectionFactory;
            this.fileStore = fileStore;

            CreateDatabase();
            MigrateDatabase();
        }

        /// <summary>
        ///     Creates the config and establish and async connection to access the sqlite database synchronous.
        /// </summary>
        /// <returns>Established SQLiteConnection.</returns>
        public SQLiteConnection GetConnection()
            => connectionFactory.GetConnection(new SqLiteConfig(DatabaseConstants.DB_NAME, false));

        /// <summary>
        ///     Creates a new Database if there isn't already an existing. If there is
        ///     one it tries to update it.
        ///     The update only happens automaticlly on the one who uses the "CreateTable" Command.
        ///     For the others the update has to be done manually.
        /// </summary>
        public void CreateDatabase()
        {
            using (var db = connectionFactory.GetConnection(DatabaseConstants.DB_NAME))
            {
                db.CreateTable<Account>();
                db.CreateTable<Category>();
                db.CreateTable<Payment>();
                db.CreateTable<RecurringPayment>();
            }
        }

        public void MigrateDatabase()
        {
            if (fileStore.Exists(DatabaseConstants.DB_NAME_OLD))
            {
                using (
                    var dbOld = connectionFactory.GetConnection(new SqLiteConfig(DatabaseConstants.DB_NAME_OLD, false)))
                {
                    using (var db = GetConnection())
                    {
                        db.InsertAll(dbOld.Table<AccountViewModel>());
                        db.InsertAll(dbOld.Table<CategoryViewModel>());

                        var recPaymentList = dbOld.Table<RecurringPaymentViewModel>().ToList();

                        var accounts = db.Table<AccountViewModel>().ToList();

                        var paymentsToMigrate = new List<PaymentViewModel>();
                        foreach (var payment in dbOld.Table<PaymentViewModel>().ToList())
                        {
                            if (accounts.Exists(x => x.Id == payment.ChargedAccountId))
                            {
                                paymentsToMigrate.Add(payment);
                            }
                        }

                        foreach (
                            var payment in paymentsToMigrate.Where(x => x.IsRecurring && (x.RecurringPaymentId == 0)))
                        {
                            payment.IsRecurring = false;
                        }

                        foreach (var recurringPayment in recPaymentList)
                        {
                            var recIdOld = recurringPayment.Id;
                            db.Insert(recurringPayment);

                            foreach (var payment in paymentsToMigrate.Where(x => x.RecurringPaymentId == recIdOld))
                            {
                                payment.RecurringPaymentId = db.Table<RecurringPaymentViewModel>().LastOrDefault().Id;
                            }
                        }

                        db.InsertAll(paymentsToMigrate);
                    }
                }

                fileStore.DeleteFile(DatabaseConstants.DB_NAME_OLD);
            }
        }

        /// <summary>
        ///     Creates the config and establish and async connection to access the sqlite database asynchronous.
        /// </summary>
        /// <returns>Established async connection.</returns>
        public SQLiteAsyncConnection GetAsyncConnection()
            => connectionFactory.GetAsyncConnection(new SqLiteConfig(DatabaseConstants.DB_NAME, false));
    }
}