using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;

namespace EventServer.Core.Domain
{
    public class Session : Entity
    {
        /// <summary>Do not use</summary>
        public Session()
        {
            Track = "None";
            Slot = "None";
        }

        public Session(UserProfile user, string title, string description, SessionLevel level, SessionCategory category)
        {
            User = user;
            UserId = user.Id;
            Title = title;
            Description = description;
            Level = level;
            Category = category;
            Track = "None";
            Slot = "None";
            Day = 1;

            Status = SessionStatus.Pending;

            Raise(new SessionCreated {PresentationId = Id});
        }

        private List<Comment> _comments;

        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Track { get; set; }
        public string Slot { get; set; }
        public SessionLevel Level { get; set; }
        public SessionStatus Status { get; set; }
        public SessionCategory Category { get; set; }
        public int Day { get; set; }


        public List<Comment> Comments
        {
            get { return _comments ?? (_comments = new List<Comment>()); }
            set { _comments = value; }
        }

        [XmlIgnore]
        public UserProfile User { get; set; }

        public string UrlTitle
        {
            get { return Title.MakeUrlFriendly(); }
        }
        public string SpeakerName
        {
            get { return User == null ? "" : User.Name; }
        }
        public string SpeakerUrlName
        {
            get { return User == null ? "" : User.UrlName; }
        }
        public string TimeSlot
        {
            get { return TimeSlots.TryGet(Slot) ?? "None"; }
        }

        public bool IsClosedForEdit()
        {
            return Status == SessionStatus.Rejected;
        }

        public void Accept()
        {
            if (Status == SessionStatus.Accepted)
                return;

            Status = SessionStatus.Accepted;
            Raise(new PresentationAccepted {PresentationId = Id});
        }

        public void Reject()
        {
            if (Status == SessionStatus.Rejected)
                return;

            Status = SessionStatus.Rejected;
            Raise(new PresentationRejected {PresentationId = Id});
        }

        public void AddComment(Comment comment)
        {
            comment.DateCreated = DateTime.Now;
            Comments.Add(comment);
            Raise(new CommentAdded {PresentationId = Id});
        }

        public void Update(string title, string description, SessionLevel level, SessionCategory category, string track, string timeSlot)
        {
            Title = title;
            Description = description;
            Level = level;
            Category = category;
            Track = string.IsNullOrEmpty(track) ? "None" : track;
            Slot = string.IsNullOrEmpty(timeSlot) ? "None" : timeSlot;

            Raise(new PresentationChanged {PresentationId = Id});
        }

        /******************************************/
        /******************************************/

        public static readonly IDictionary<string, string> TimeSlots = new Dictionary<string, string>
            {
                {"None", "None"},
                {"1", "08:50 AM - 09:50 AM"},
                {"2", "10:00 AM - 11:00 AM"},
                {"3", "11:10 AM - 12:10 PM"},
                {"4", "01:30 PM - 02:30 PM"},
                {"5", "02:40 PM - 03:40 PM"},
                {"6", "03:50 PM - 04:50 PM"},
            };

    }

    public static class PresentationExtensions
    {
        public static Session[] FindAcceptedPresentations(this IRepository repository)
        {
            var speakers = repository.Find<UserProfile>().ToDictionary(x => x.Id);

            return repository.Find<Session>()
                .Where(x => x.Status == SessionStatus.Accepted)
                .Each(x => x.User = speakers[x.UserId])
                .OrderBy(x => x.User.Name)
                .ThenBy(x => x.Title)
                .ToArray();
        }
    }
}