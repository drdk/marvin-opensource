using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using DR.Marvin.Model;

namespace DR.Marvin.Simulator
{
    public class InMemoryCommandRepository : ICommandRepository
    {
        private readonly IDictionary<string, Command> _dict = new ConcurrentDictionary<string, Command>();
        
        private static string CommandToId(Command cmd)
        {
            return $"{cmd.Urn}#{cmd.Type}";
        }
        public IEnumerable<Command> GetAll()
        {
            return _dict.Values;
        }

        public void Remove(Command command)
        {
            _dict.Remove(CommandToId(command));
        }

        public void Add(Command command)
        {
            var id = CommandToId(command);
            if (_dict.ContainsKey(id))
                throw new Exception("cmd already exists");
            _dict[id] = Mapper.Map<Command>(command);
        }
        public void Reset()
        {
            _dict.Clear();
        }
        
        public string GetEnvironment()
        {
            return "INMEMORY";
        }
    }
}