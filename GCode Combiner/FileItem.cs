using System;
using System.IO;

namespace GCode_Combiner
{
    public class FileItem
    {
        public bool Selected { get; set; }

        public string FilePath { get; set; }

        public string FileName
        {
            get
            {
                return Path.GetFileName(FilePath);
            }
        }
    }
}