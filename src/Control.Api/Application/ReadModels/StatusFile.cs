using Metrica.Contracts;

namespace Control.Api.Application.ReadModels;

internal sealed record StatusFile(EstadoCarga Status, string FileName);
