using Metrica.Shared;

namespace Control.Api.Application.ReadModels;

internal sealed record StatusFile(LoadStatus Status, string FileName);
