using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcase.Models.Gallery
{
    public class GalleryItemManager
    {
        #region Singleton
        private static volatile GalleryItemManager instance;
        private static object syncRoot = new Object();
        public static GalleryItemManager Default
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new GalleryItemManager();
                    }
                }

                return instance;
            }
        }
        #endregion

        #region Properties

        private const int MediaFilesListIndex = 0;
        private const int MediaFoldersListIndex = 1;
        private const int ItemsListIndex = 2;

        private Dictionary<string, List<List<GalleryItem>>> _cache = new Dictionary<string, List<List<GalleryItem>>>();

        //holds the current navigation path of the gallery 
        private List<string> _NavigationHierarchy = new List<string>();

        //holds the media items for the current folder
        private List<GalleryItem> _MediaFiles = new List<GalleryItem>();
        public IReadOnlyList<GalleryItem> MediaFiles
        {
            get { return _MediaFiles; }
        }

        private List<GalleryItem> _MediaFolders = new List<GalleryItem>();

        //holds the groups
        private List<GalleryItem> _items = new List<GalleryItem>();
        //returns a readonly list for binding to a group
        public IReadOnlyList<GalleryItem> Items
        {
            get { return _items; }
        }

        public string FolderIconPath { get; set; }

        public string RootFolder
        {
            get
            {
                return (_NavigationHierarchy.Count > 0 ?  _NavigationHierarchy[0] : null);
            }
            set
            {
                SetRootFolder(value);
            }
        }

        #endregion

        #region Constructors

        private GalleryItemManager()
        {

        }

        #endregion

        #region Getters & Setters

        public void SetRootFolder(string aRootFolderPath)
        {
            this._NavigationHierarchy.Clear();
            this.AddFolder(aRootFolderPath);
        }
        public void AddFolder(string aPath)
        {
            //add it to the navigation tree
            this._NavigationHierarchy.Add(aPath);
            //get the new set of media items for the new current directory
            GetMediaForCurrentDirectory();
        }

        public string CurrentFolder()
        {
            string result = null;
            if (this._NavigationHierarchy.Count > 0)
            {
                string fullFolderPath = this._NavigationHierarchy.Last();
                result = GetNameFromPath(fullFolderPath);
            }

            return result;
        }

        #endregion

        #region GalleryItem Processing

        private void GetMediaForCurrentDirectory()
        {
            if (!_cache.ContainsKey(_NavigationHierarchy.Last()))
            {
                List<GalleryItem> foldersList = new List<GalleryItem>();
                //get all the folders in the current directory
                string[] folders = Directory.GetDirectories(_NavigationHierarchy.Last());
                //process the folders
                foreach (string currentFolder in folders)
                {
                    //process the folder
                    GalleryItem newItem = GalleryItemForFolder(_NavigationHierarchy.Last(), currentFolder);
                    //add the item to our list
                    foldersList.Add(newItem);
                }

                List<GalleryItem> filesList = new List<GalleryItem>();
                //get all the files for the current directory
                string[] files = Directory.GetFiles(_NavigationHierarchy.Last());
                //process the files
                foreach (string currentFile in files)
                {
                    //make sure the file is a media file before we process it
                    if (FileIsMedia(currentFile))
                    {
                        //if so, process the file
                        GalleryItem newItem = GalleryItemForFile(_NavigationHierarchy.Last(), currentFile);
                        //add the item to our list
                        filesList.Add(newItem);
                    }
                }

                List<GalleryItem> allItemsList = new List<GalleryItem>();
                //add the groups back if they have items, refreshing the UI in the process
                if (foldersList.Count > 0)
                {
                    foreach (GalleryItem current in foldersList)
                        allItemsList.Add(current);
                }
                if (filesList.Count > 0)
                {
                    foreach (GalleryItem current in filesList)
                        allItemsList.Add(current);
                }

                List<List<GalleryItem>> cached = new List<List<GalleryItem>>();
                cached.Add(filesList);
                cached.Add(foldersList);
                cached.Add(allItemsList);
                _cache[_NavigationHierarchy.Last()] = cached;
            }

            List<List<GalleryItem>> _cachedItems = _cache[_NavigationHierarchy.Last()];
            _items = _cachedItems.ElementAt(ItemsListIndex);
            _MediaFolders = _cachedItems.ElementAt(MediaFoldersListIndex);
            _MediaFiles = _cachedItems.ElementAt(MediaFilesListIndex);
        }

        private bool FileIsMedia(string aFile)
        {
            bool result = false;

            if (FileIsImage(aFile) || FileIsVideo(aFile))
            {
                result = true;
            }

            return result;
        }

        private bool FileIsImage(string aFile)
        {
            bool result = false;

            if (aFile.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase) ||
                aFile.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase) ||
                aFile.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        private bool FileIsVideo(string aFile)
        {
            bool result = false;

            if (aFile.EndsWith(".mp4", StringComparison.CurrentCultureIgnoreCase))
            {
                result = true;
            }

            return result;
        }

        private GalleryItem GalleryItemForFile(string aParentFolder, string aFile)
        {
            string path = aParentFolder;
            string name = this.GetNameFromPath(aFile);
            string description = "TODO - File description not done yet.";
            bool isImage = FileIsImage(aFile);
            GalleryItem.GalleryItemType type = (isImage ? GalleryItem.GalleryItemType.GalleryMediaTypeImage : GalleryItem.GalleryItemType.GalleryMediaTypeVideo);

            return new GalleryItem(path, type, name, description);
        }

        private GalleryItem GalleryItemForFolder(string aParentFolder, string aFolder)
        {
            string path = aParentFolder;
            string name = this.GetNameFromPath(aFolder);
            string description = "TODO - Folder description not done yet.";
            GalleryItem.GalleryItemType type = GalleryItem.GalleryItemType.GalleryMediaTypeFolder;

            return new GalleryItem(path, type, name, description);
        }

        #endregion

        #region Navigation

        public bool CanGoBack()
        {
            return (this._NavigationHierarchy.Count > 1);
        }

        public void GoBack()
        {
            //check if we can go back
            if (CanGoBack())
            {
                //remove the top most folder from the tree
                this._NavigationHierarchy.Remove(this._NavigationHierarchy.Last());

                //get the new set of media items for the list
                GetMediaForCurrentDirectory();
            }
            else
            {
                Debug.WriteLine("GalleryItemManager - WARN - Cannot go back.");
            }
        }

        public void GoInFolder(GalleryItem aItem)
        {
            AddFolder(aItem.ParentFolderPath + "/" + aItem.Title);
        }

        #endregion

        #region Helpers

        public string GetNameFromPath(string aPath)
        {
            string result = null;
            int slashIndex = aPath.LastIndexOf("\\");
            if (slashIndex != -1 && slashIndex < aPath.Length - 2)
            {
                string name = aPath.Substring(slashIndex + 1);
                result = name;
            }

            return result;
        }

        #endregion
    }
}
