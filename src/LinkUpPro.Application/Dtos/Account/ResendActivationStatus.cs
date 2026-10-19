namespace LinkUpPro.Application.Dtos.Account
{
    public enum ResendActivationStatus
    {
        Processed, // se procesó (exista o no la cuenta: no se revela)
        Throttled  // ya hubo una solicitud para ese usuario hace menos de 5 minutos
    }
}
