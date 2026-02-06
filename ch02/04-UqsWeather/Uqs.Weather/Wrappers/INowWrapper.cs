namespace Uqs.Weather.Wrappers;

//The interface defines the abstraction that 
//the controller depends on, while the concrete wrapper 
//provides the production implementation; 
//this separation allows unit tests to substitute controlled 
//implementations without changing application code.
public interface INowWrapper
{
    DateTime Now { get; }
}

