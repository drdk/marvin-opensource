using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Repositories
{
    public class SqlCommandRepository : ICommandRepository
    {
        private static TransactionScope CreateScope()
        {
            return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
            {
                IsolationLevel = IsolationLevel.Snapshot,
                Timeout = TimeSpan.FromSeconds(30)
            });
        }

        public IEnumerable<Command> GetAll()
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var commands = db.command;

                return commands.Select(Mapper.Map<Command>).ToList();
            }
        }

        public void Remove(Command command)
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var com = Mapper.Map<command>(command);

                db.command.Attach(com);
                db.command.Remove(com);

                db.SaveChanges();
                scope.Complete();
            }
        }

        public void Add(Command command)
        {
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                db.command.Add(Mapper.Map<command>(command));

                db.SaveChanges();
                scope.Complete();
            }
        }

        /// <summary>
        /// :warning: Only use for unit testing on local db.
        /// </summary>
        internal void Reset()
        {
            //Resetting commands - dangerzone! :fire:
            using (var scope = CreateScope())
            using (var db = new MarvinEntities())
            {
                var connectionString = db.Database.Connection.ConnectionString;
                if (!connectionString.Contains("MarvinLocal") || !connectionString.Contains("user id=nunit"))
                    throw new Exception("Reset method is only allow for MarvinLocal and nunit user.");
                db.command.RemoveRange(db.command);
                db.SaveChanges();
                scope.Complete();
            }
        }
    }
}