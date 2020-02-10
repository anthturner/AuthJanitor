using System.Threading.Tasks;

namespace AuthorizationJanitor.RotationActions
{
    public interface IRotation
    {
        Task<JanitorConfigurationEntity> Execute(JanitorConfigurationEntity entity);
    }
}
