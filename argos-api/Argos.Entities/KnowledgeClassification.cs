namespace Argos.Entities;

/// <summary>Resultado de la regla conocida/nueva: si es conocida, por qué y cuál es el mejor documento.</summary>
public sealed record KnowledgeClassification(bool IsKnown, string Reason, ScoredIncident? Best)
{
    public static readonly KnowledgeClassification New = new(false, KnowledgeMatchReasons.None, null);
}
