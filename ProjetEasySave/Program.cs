using System.Text.Json;
using ProjetEasySave.Utils;
using ProjetEasySave.View;

namespace ProjetEasySave;
class Program
{
    static void Main(string[] args)
    {
        var view = new View.View();
        view.run();
    }
}