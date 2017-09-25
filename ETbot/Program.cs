using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETbot {
    class Program {
        static void Main(string[] args) {
            while (true) {
                try {
                    ETbot.Connect("foxdev.co", 12345);
                }
                catch (EndOfStreamException) {

                }
            }
        }
    }
}
