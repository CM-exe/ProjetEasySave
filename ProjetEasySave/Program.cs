
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello World!");
        if (args.Length > 0) {
            Console.WriteLine(args[0]);
        }
        Console.WriteLine("Goodbye World!");
    }
}