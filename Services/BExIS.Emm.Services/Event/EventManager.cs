using BExIS.Dlm.Entities.MetadataStructure;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vaiona.Persistence.Api;
using Vaiona.Entities;
using E = BExIS.Emm.Entities.Event;
using BExIS.Security.Services.Objects;

namespace BExIS.Emm.Services.Event
{
    public class EventManager: IEntityStore
    {
        public EventManager()
        {
            IUnitOfWork uow = this.GetUnitOfWork();
            this.EventRepo = uow.GetReadOnlyRepository<E.Event>();
        }

        #region Data Readers

        public IReadOnlyRepository<E.Event> EventRepo { get; private set; }

        #endregion

        #region Methods


        /// <summary>
        /// Creates an EventRegistration <seealso cref="EventRegistration"/> and persists the entity in the database.
        /// </summary>
        public E.Event CreateEvent(string name, DateTime startDate, DateTime deadline, int participantsLimitation, bool editAllowed, string logInPassword, MetadataStructure metadataStructure)
        {
            E.Event newEvent = new E.Event();
            newEvent.Name = name;
            newEvent.MetadataStructure = metadataStructure;
            newEvent.StartDate = startDate;
            newEvent.Deadline = deadline;
            newEvent.ParticipantsLimitation = participantsLimitation;
            newEvent.EditAllowed = editAllowed;
            newEvent.LogInPassword = logInPassword;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                repo.Put(newEvent);
                uow.Commit();
            }

            return newEvent;
        }

        /// <summary>
        /// If the <paramref name="Activity"/> is not associated to any <see cref="Event"/>, the method deletes it from the database.
        /// </summary>
        public bool DeleteEvent(E.Event eEvent)
        {
            Contract.Requires(eEvent != null);
            Contract.Requires(eEvent.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                eEvent = repo.Reload(eEvent);
                repo.Delete(eEvent);
                uow.Commit();
            }

            return true;
        }

        public E.Event UpdateEvent(E.Event eEvent)
        {
            Contract.Requires(eEvent != null);
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                repo.Put(eEvent);
                uow.Commit();
            }
            return eEvent;

        }

        public IQueryable<E.Event> GetAllEvents()
        {
            return EventRepo.Query();
        }

        public E.Event GetEventById(long id)
        {
            return EventRepo.Query(u => u.Id == id).FirstOrDefault();
        }

        public List<EntityStoreItem> GetEntities()
        {
            throw new NotImplementedException();
        }


        #endregion
    }

}
