using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcase.Models.Games
{
    class GameConfig
    {
        public string Title = "My Game";
        public string Executable = "MyGame.exe";
        public string Icon = "icon.png";
        public string Screenshot = "screen.png";
        public string DescriptionFile = "description.txt";

        public bool HasRequiredFields()
        {
            return (Title != null && Executable != null && Icon != null && Screenshot != null && DescriptionFile != null);
        }
    }
}
