using System;

namespace ETbot {
    class Program {
        static void Main(string[] args) {
            while (true) {
                ETbot.Connect("foxdev.co", 12345);
            }
        }
    }
}
