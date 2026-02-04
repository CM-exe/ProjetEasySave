using System;
using System.Collections.Generic;
using System.Text;

namespace ProjetEasySave.Model
{
    interface ISaveStrategy
    {
        /*
         * Interface for save strategies
         */

        // Interface methods
        bool doSave(string sourcePath, string destinationPath);
    }
}
