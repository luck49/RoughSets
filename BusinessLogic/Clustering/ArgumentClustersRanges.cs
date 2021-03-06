﻿using System.Collections.Generic;

namespace BusinessLogic.Clustering
{
    public class ArgumentClustersRanges
    {
        public List<ClusterRange> ClusterRanges { get; private set; }

        public ArgumentClustersRanges(List<ClusterRange> clusterRanges)
        {
            ClusterRanges = clusterRanges;
        }
    }
}
