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

        public List<EntityStoreItem> GetEntities()
        {
            return GetEntities(0, 0);
        }

        public List<EntityStoreItem> GetEntities(int skip, int take)
        {
            bool withPaging = (take >= 0);
            var entities = new List<EntityStoreItem>();

            using (EventManager eManger = new EventManager())
            {

                List<E.Event> events = new List<E.Event>();
                if (withPaging)
                {
                    events = eManger.EventRepo.Query().Skip(skip).Take(take).ToList();
                }
                else
                {
                    events = eManger.EventRepo.Query().ToList();
                }
                
                foreach (E.Event e in events){
                    entities.Add(new EntityStoreItem() { Id = e.Id, Title = e.Name });

                }

                return entities;
            }
        }

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

        public int CountEntities()
        {
            using (EventManager eManger = new EventManager())
            {
                return eManger.EventRepo.Query().Count();
            }
        }

        public bool Exist(long id)
        {
            throw new NotImplementedException();
        }
    }
}
