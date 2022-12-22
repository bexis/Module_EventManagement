using BExIS.Dlm.Entities.MetadataStructure;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Vaiona.Persistence.Api;
using E = BExIS.Emm.Entities.Event;


namespace BExIS.Emm.Services.Event
{
    public class EventManager: IDisposable
    {
        private IUnitOfWork guow = null;
        public EventManager()
        {
            IUnitOfWork guow = this.GetUnitOfWork();
            this.EventRepo = guow.GetReadOnlyRepository<E.Event>();
        }

        private bool isDisposed = false;
        ~EventManager()
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
                    if (guow != null)
                        guow.Dispose();
                    isDisposed = true;
                }
            }
        }


        #region Data Readers

        public IReadOnlyRepository<E.Event> EventRepo { get; private set; }

        #endregion

        #region Methods

        public IQueryable<E.Event> GetAllEvents()
        {
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                return repo.Query();
            }
        }

        public E.Event GetEventById(long id)
        {
            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                return repo.Query(u => u.Id == id).FirstOrDefault();
            }
        }


        /// <summary>
        /// Creates an EventRegistration <seealso cref="EventRegistration"/> and persists the entity in the database.
        /// </summary>
        public E.Event CreateEvent(string name, string eventDate, string importantInformation, string location, string mailInformation, string eventLanguage, DateTime startDate, DateTime deadline, int participantsLimitation, bool waitingList, int waitingListLimitation, bool editAllowed, bool closed, string logInPassword, string emailBCC, string emailCC, string emailReply, MetadataStructure metadataStructure, string javaScriptPath)
        {
            E.Event newEvent = new E.Event();
            newEvent.Name = name;
            newEvent.EventDate = eventDate;
            newEvent.ImportantInformation = importantInformation;
            newEvent.Location = location;
            newEvent.MailInformation = mailInformation;
            newEvent.EventLanguage = eventLanguage;
            newEvent.MetadataStructure = metadataStructure;
            newEvent.StartDate = startDate;
            newEvent.Deadline = deadline;
            newEvent.ParticipantsLimitation = participantsLimitation;
            newEvent.WaitingList = waitingList;
            newEvent.WaitingListLimitation = waitingListLimitation;
            newEvent.EditAllowed = editAllowed;
            newEvent.Closed = closed;
            newEvent.LogInPassword = logInPassword;
            newEvent.EmailBCC = emailBCC;
            newEvent.EmailCC = emailCC;
            newEvent.EmailReply = emailReply;
            newEvent.JavaScriptPath = javaScriptPath;

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                repo.Put(newEvent);
                uow.Commit();
            }

            return newEvent;
        }

        /// <summary>
        /// If the <paramref name="eEvent"/> is not associated to any <see cref="Event"/>, the method deletes it from the database.
        /// </summary>
        public bool DeleteEvent(E.Event eEvent)
        {
            Contract.Requires(eEvent != null);
            Contract.Requires(eEvent.Id >= 0);

            using (IUnitOfWork uow = this.GetUnitOfWork())
            {
                IRepository<E.Event> repo = uow.GetRepository<E.Event>();
                var latest = repo.Reload(eEvent);
                repo.Delete(latest);
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

 

        #endregion
    }

}
