using E = BExIS.Emm.Entities.Event;
using BExIS.Security.Entities.Subjects;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Vaiona.Persistence.Api;
using System.Xml.Linq;

namespace BExIS.Emm.Services.Event
{
    public class EventRegistrationManager : IDisposable
    {
        private IUnitOfWork uow = null;
        public EventRegistrationManager()
        {
            IUnitOfWork uow = this.GetUnitOfWork();
            this.EventRegistrationRepo = uow.GetReadOnlyRepository<E.EventRegistration>();
        }


        private bool isDisposed = false;
        ~EventRegistrationManager()
        {
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    if (uow != null)
                        uow.Dispose();
                    isDisposed = true;
                }
            }
        }

        #region Data Readers

        public IReadOnlyRepository<E.EventRegistration> EventRegistrationRepo { get; private set; }

        #endregion

        #region Methods


        /// <summary>
        /// Creates an EventRegistration <seealso cref="EventRegistration"/> and persists the entity in the database.
        /// </summary>
        public E.EventRegistration CreateEventRegistration(XmlDocument data, E.Event e, User user, bool deleted, string token)
        {
            E.EventRegistration eventRegistration = new E.EventRegistration();
            eventRegistration.Data = data;
            eventRegistration.Deleted = deleted;
            eventRegistration.Event = e;
            eventRegistration.Person = user;
            eventRegistration.Token= token;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.EventRegistration> repo = uow.GetRepository<E.EventRegistration>();
                repo.Put(eventRegistration);
                uow.Commit();
            }

            return eventRegistration;
        }

        /// <summary>
        /// If the <paramref name="Activity"/> is not associated to any <see cref="Event"/>, the method deletes it from the database.
        /// </summary>
        public bool DeleteEventRegistration(E.EventRegistration eventRegistration)
        {
            Contract.Requires(eventRegistration != null);
            Contract.Requires(eventRegistration.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.EventRegistration> repo = uow.GetRepository<E.EventRegistration>();
                eventRegistration = repo.Reload(eventRegistration);
                repo.Delete(eventRegistration);
                uow.Commit();
            }

            return true;
        }

        public E.EventRegistration UpdateEventRegistration(E.EventRegistration eventRegistration)
        {
            Contract.Requires(eventRegistration != null);
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.EventRegistration> repo = uow.GetRepository<E.EventRegistration>();
                repo.Merge(eventRegistration);
                var merged = repo.Get(eventRegistration.Id);
                repo.Put(merged);
                uow.Commit();
            }
            return eventRegistration;

        }

        public List<E.EventRegistration> GetAllRegistrationsByEvent(long id)
        {
            return EventRegistrationRepo.Query(a=>a.Event.Id == id).ToList();
        }

        public List<E.EventRegistration> GetAllRegistrationsNotDeletedByEvent(long id)
        {
            return EventRegistrationRepo.Query(a => a.Event.Id == id && a.Deleted == false).ToList();
        }

        public List<E.EventRegistration> GetRegistrationByUserAndEvent(long userId, long eventId)
        {
            return EventRegistrationRepo.Query(a => a.Event.Id == eventId && a.Person.Id == userId).ToList();
        }

        public E.EventRegistration GetRegistrationByRefIdAndEvent(string ref_id, long eventId)
        {
            return EventRegistrationRepo.Query(a => a.Event.Id == eventId && a.Token == ref_id).FirstOrDefault();
        }


        public int GetNumerOfRegistrationsByEvent(long id)
        {
            return EventRegistrationRepo.Query(a => a.Event.Id == id && a.Deleted == false).Count();
        }

        #endregion
    }
}
