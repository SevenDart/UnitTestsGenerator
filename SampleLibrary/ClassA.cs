using System;

namespace SampleLibrary
{
    public class ClassA
    {
        private IDependency _dependency;
        
        public ClassA(IDependency dependency, int x)
        {
            _dependency = dependency;
        }
        
        
        public bool Lie()
        {
            return false;
        }

        public int Return3(int x)
        {
            return 3;
        }

        public void SayTime(DateTime now)
        {
            Console.WriteLine(now);
        }
    }
}