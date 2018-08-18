using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmokeMonster_Romset_Parser
{
    class Rom
    {
        private string filename;
        private string targetpath;
        private string checksum;
        private bool shaalreadymatched;

        public string FileName
        {
            get { return filename; }
            set { filename = value; }
        }

        public string Checksum
        {
            get { return checksum; }
            set { checksum = value; }
        }

        public string Targetpath
        {
            get { return targetpath; }
            set { targetpath = value; }
        }


        public bool ShaAlreadyMatched
        {
            get { return shaalreadymatched; }
            set { shaalreadymatched = value; }
        }
    }
}
