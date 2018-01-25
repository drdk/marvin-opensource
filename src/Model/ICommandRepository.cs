using System.Collections.Generic;
#pragma warning disable 1591

namespace DR.Marvin.Model
{
    public interface ICommandRepository
    {
        IEnumerable<Command> GetAll();

        void Remove(Command command);

        void Add(Command command);
    }
}