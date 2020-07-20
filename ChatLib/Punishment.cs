using ChatLib.DataStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChatLib.Administrator
{
    public struct Punishment
    {
        public IPAddress ClientAddress { get; private set; }
        public RevokedPerms RevokedPerms { get; private set; }
        public bool IsTempBan { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public Punishment(IPAddress clientAddress, RevokedPerms revokedPerms, int duration = -1) : this()
        {
            ClientAddress = clientAddress;
            RevokedPerms = revokedPerms;

            if (duration < 0)
            {
                IsTempBan = false;
                StartDate = DateTime.Now;
                EndDate = DateTime.Now;
            }
            else
            {
                IsTempBan = true;
                StartDate = DateTime.UtcNow;
                EndDate = StartDate.AddHours(duration);
            }
        }

        public Punishment(IPAddress clientAddress, RevokedPerms revokedPerms, bool isTempBan, DateTime startDate, DateTime endDate) : this()
        {
            ClientAddress = clientAddress;
            RevokedPerms = revokedPerms;
            IsTempBan = isTempBan;
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
