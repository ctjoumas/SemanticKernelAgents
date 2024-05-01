namespace NGAAgents.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class UserManualDetails
    {
        public string ChunkId { get; set; }

        public string Chunk { get; set; }

        public string Title { get; set; }

        public double Score { get; set; }

        public double RerankerScore { get; set; }
    }
}
