using MuranoApp.Options;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MuranoApp.Models
{

    public sealed class ApiBlockResponse
    {
        public ApiBlockMode Mode { get; set; }
        public string? Start { get; set; }
        public string? End { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = default!;
        public List<string> IgnoredPaths { get; set; } = new();
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public string? UpdatedBy { get; set; }
    }
    public sealed class UpdateApiBlockRequest : IValidatableObject
    {
        public ApiBlockMode Mode { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int? StatusCode { get; set; }
        public string? Message { get; set; }
        public List<string>? IgnoredPaths { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var errors = new List<ValidationResult>();

            if (Mode == ApiBlockMode.Schedule)
            {
                if (!Start.HasValue)
                {
                    errors.Add(new ValidationResult(
                        "Para o modo Schedule, Start é obrigatório.",
                        new[] { nameof(Start) }));
                }

                if (!End.HasValue)
                {
                    errors.Add(new ValidationResult(
                        "Para o modo Schedule, End é obrigatório.",
                        new[] { nameof(End) }));
                }

                if (Start.HasValue && End.HasValue && End <= Start)
                {
                    errors.Add(new ValidationResult(
                        "End deve ser maior que Start.",
                        new[] { nameof(Start), nameof(End) }));
                }
            }

            if (Mode != ApiBlockMode.Schedule &&
                (Start.HasValue || End.HasValue))
            {
                errors.Add(new ValidationResult(
                    "Start e End só podem ser enviados no modo Schedule.",
                    new[] { nameof(Start), nameof(End) }));
            }

            if (IgnoredPaths is not null)
            {
                var containsAdmin = IgnoredPaths.Any(path =>
                    string.Equals(NormalizePath(path), ApiBlockDefaults.AdminPath, StringComparison.OrdinalIgnoreCase));

                if (!containsAdmin)
                {
                    errors.Add(new ValidationResult(
                        "O caminho '/admin' é obrigatório em ignoredPaths e não pode ser removido.",
                        new[] { nameof(IgnoredPaths) }));
                }
            }

            return errors;
        }

        private static string NormalizePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            path = path.Trim();

            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            return path.TrimEnd('/');
        }
    }
}
