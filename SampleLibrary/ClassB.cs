namespace SampleLibrary
{
    public class ClassB
    {
        private IDependency _dependency;
        private IDependency _dependency2;
        
        public ClassB(IDependency dependency, IDependency dependency2)
        {
            _dependency = dependency;
            _dependency2 = dependency2;
        }

        public void TestAction()
        {
            
        }
    }
}