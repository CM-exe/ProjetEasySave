using System;
using System.Collections.Generic;
using System.Text;

namespace ProjetEasySave.Model
{
    internal class DifferentialSave : ISaveStrategy
    {
        /*
         * Differential Save implementation of the ISaveStrategy interface.
         */

        // doSave method implementation for Differential Save
        public bool doSave(string sourcePath, string destinationPath)
        {
            return true;
        }
    }
}
