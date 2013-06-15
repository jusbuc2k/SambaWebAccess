using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Website
{
    public class FileModel
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public long Length { get; set; }
        public DateTime Modified { get; set; }
        public bool IsFile { get; set; }
    }
}