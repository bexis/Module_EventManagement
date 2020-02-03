using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BExIS.Security.Services.Objects;
using E = BExIS.Emm.Entities.Event;



namespace BExIS.Emm.Services.Event
{
    public class EventStore : IEntityStore
    {
        public IQueryable<E.Event> GetAllEnties()
        {
            using (EventManager eManger = new EventManager())
            {
                return eManger.EventRepo.Query();
            }
        }

        public E.Event GetEventById(long id)
        {
            using (EventManager eManger = new EventManager())
            {
                return eManger.EventRepo.Query(u => u.Id == id).FirstOrDefault();
            }
        }

        public List<EntityStoreItem> GetEntities()
        {
            throw new NotImplementedException();
        }

        public string GetTitleById(long id)
        {
            throw new NotImplementedException();
        }

        public bool HasVersions()
        {
            throw new NotImplementedException();
        }

        public int CountVersions(long id)
        {
            throw new NotImplementedException();
        }

        public List<EntityStoreItem> GetVersionsById(long id)
        {
            throw new NotImplementedException();
        }


    }
}
