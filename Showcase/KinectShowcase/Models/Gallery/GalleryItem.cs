using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace KinectShowcase.Models.Gallery
{
    public class GalleryItem
    {
        public enum GalleryItemType
        {
            GalleryMediaTypeFolder,
            GalleryMediaTypeImage,
            GalleryMediaTypeVideo,
        }

        public string ParentFolderPath { get; private set; }
        public GalleryItemType Type { get; private set; }
        public string Title { get; private set; }
        public string DescriptionPath { get; private set; }
        public string MediaPath { get; private set; }

        public GalleryItem(string aPath, GalleryItemType aType, string aTitle, string aDescription)
        {
            this.ParentFolderPath = aPath;
            this.Type = aType;
            this.Title = aTitle;
            this.DescriptionPath = aDescription;

            if (this.Type == GalleryItemType.GalleryMediaTypeFolder)
            {
                this.MediaPath = GalleryItemManager.Default.FolderIconPath;
            }
            else if (this.Type == GalleryItemType.GalleryMediaTypeImage)
            {
                this.MediaPath = ParentFolderPath + "\\" + Title;
            }
            else
            {
                Debug.WriteLine("GalleryItem - not handling icon types for other items");
                this.MediaPath = "";
            }
        }
    }
}
