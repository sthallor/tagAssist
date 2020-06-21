using Common.Models.Reporting;

namespace Batch.FactoryStuff
{
    public interface IEgnCheck
    {
        bool Check(EgnServer egnServer);
    }
}