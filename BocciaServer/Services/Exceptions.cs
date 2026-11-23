namespace BocciaServer.Services;

/// <summary>La acción no está permitida por las reglas o por el estado del partido (HTTP 409).</summary>
public class GameRuleException(string message) : Exception(message);

/// <summary>El recurso solicitado no existe (HTTP 404).</summary>
public class NotFoundException(string message) : Exception(message);
