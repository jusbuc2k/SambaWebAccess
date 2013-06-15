using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Website.Controllers
{
    public class BrowseController : Controller
    {
        private IDisposable BeginImpersonation()
        {
            return null;
            //return new System.Security.Principal.WindowsIdentity(System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName).Impersonate();
        }

        private bool HasPathAccess(string path)
        {
            return true;
        }

        private string EncodeFileName(string path)
        {
            path = path.Replace(" ", "%20");
            return path;
        }
        

        public ActionResult Index()
        {
            var model = new List<RootPath>();
            var roots = System.Configuration.ConfigurationManager.AppSettings["Roots"].Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);

            foreach (var root in roots)
            {
                model.Add(new RootPath() { Path = root });
            }

            return View(model);
        }

        public ActionResult List(string path, string filter = null)
        {
            var model = new List<FileModel>();
            
            using (this.BeginImpersonation())
            {
                var directory = new System.IO.DirectoryInfo(path);

                if (!this.HasPathAccess(directory.FullName))
                {
                    return null;
                }

                IEnumerable<DirectoryInfo> dirs;
                IEnumerable<FileInfo> files;

                if (filter == null || filter.Length <= 0)
                {
                    files = directory.GetFiles();
                    dirs = directory.GetDirectories();
                }
                else
                {
                    files = directory.GetFiles(filter);
                    dirs = directory.GetDirectories();
                }

                foreach (var dir in dirs)
                {
                    if (dir.Name.StartsWith("."))
                        continue;
                    model.Add(new FileModel() { Name = dir.Name, FullName = dir.FullName, Length = 0, Modified = dir.LastWriteTime });
                }

                foreach (var file in files)
                {
                    if (file.Name.StartsWith("."))
                        continue;
                    model.Add(new FileModel() { Name = file.Name, FullName = file.FullName, Length = file.Length, Modified = file.LastWriteTime, IsFile = true});
                }
            }

            return View(model);
        }

        public ActionResult Download(string file, string disposition = "attachment")
        {
            using (this.BeginImpersonation())
            {
                var fileInfo = new System.IO.FileInfo(file);

                if (!this.HasPathAccess(fileInfo.FullName))
                {
                    return null;
                }
                
                this.Response.Clear();
                this.Response.BufferOutput = false;
                this.Response.Cache.SetNoStore();
                this.Response.AddHeader("Content-Length", fileInfo.Length.ToString());

                if (disposition == "attachment")
                {
                    this.Response.AddHeader("Content-Disposition", string.Concat("attachment; filename=", EncodeFileName(fileInfo.Name)));
                }
                else
                {
                    this.Response.AddHeader("Content-Disposition", string.Concat("inline; filename=", EncodeFileName(fileInfo.Name)));
                }

                var buffer = new byte[10240];
                int byteCount = 0;
                using (var inStream = fileInfo.OpenRead())
                {
                    while (true)
                    {
                        byteCount = inStream.Read(buffer, 0, buffer.Length);
                        if (byteCount <= 0)
                            break;
                        this.Response.OutputStream.Write(buffer, 0, byteCount);
                    }
                }

                this.Response.End();
                return new EmptyResult();
            }
        }

        public ActionResult Thumb(string file, string size="l")
        {
            this.Response.Cache.SetCacheability(HttpCacheability.Public);
            this.Response.Cache.SetMaxAge(new TimeSpan(24, 0, 0));
            this.Response.ContentType = "image/jpeg";
            System.Drawing.Bitmap thumb = null;

            using (this.BeginImpersonation())
            {
                var fi = new FileInfo(file);
                if (!fi.Exists)
                    return null;

                var shellFile = Microsoft.WindowsAPICodePack.Shell.ShellFile.FromFilePath(file);
               
                if (!this.HasPathAccess(shellFile.Path))
                {
                    return null;
                }

                if (size == "l")
                {
                    thumb = shellFile.Thumbnail.LargeBitmap;
                }
                else if (size == "s")
                {
                    thumb = shellFile.Thumbnail.SmallBitmap;
                }
                else
                {
                    thumb = shellFile.Thumbnail.Bitmap;
                }

                if (thumb == null)
                {
                    return new HttpStatusCodeResult(404);
                    //thumb.Save(this.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                else
                {
                    thumb.Save(this.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    this.Response.Flush();
                    this.Response.End();
                }

                return new EmptyResult();
            }
        }
    }
}
