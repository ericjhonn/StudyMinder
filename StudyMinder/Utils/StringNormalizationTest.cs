using System;
using StudyMinder.Utils;

namespace StudyMinder.Utils
{
    /// <summary>
    /// Teste para verificar se StringNormalizationHelper está funcionando corretamente
    /// </summary>
    public static class StringNormalizationTest
    {
        public static void TestRemoveAccents()
        {
            Console.WriteLine("=== TESTE: RemoveAccents ===");
            
            var tests = new[]
            {
                ("Português", "Portugues"),
                ("São Paulo", "Sao Paulo"),
                ("Função", "Funcao"),
                ("Procurador", "Procurador"),
                ("Caixa Econômica", "Caixa Economica"),
                ("PORTUGUÊS", "PORTUGUES"),
                ("ç", "c"),
                ("Ç", "C"),
            };

            foreach (var (input, expected) in tests)
            {
                var result = StringNormalizationHelper.RemoveAccents(input);
                bool pass = result == expected;
                Console.WriteLine($"{(pass ? "✓" : "✗")} RemoveAccents('{input}') = '{result}' (esperado: '{expected}')");
            }
        }

        public static void TestContainsIgnoreCaseAndAccents()
        {
            Console.WriteLine("\n=== TESTE: ContainsIgnoreCaseAndAccents ===");
            
            var tests = new[]
            {
                ("Português", "portugues", true),
                ("Português", "PORTUGUES", true),
                ("São Paulo", "sao paulo", true),
                ("Função", "funcao", true),
                ("Procurador", "procurador", true),
                ("Caixa Econômica", "caixa", true),
                ("Caixa Econômica", "economica", true),
                ("Analista", "analista", true),
                ("Analista", "ANALISTA", true),
                ("Fundação Cesgranrio", "fundacao cesgranrio", true),
                ("Fundação Cesgranrio", "cesgranrio", true),
                ("Direito Constitucional", "direito", true),
                ("Direito Constitucional", "CONSTITUCIONAL", true),
                ("Teste", "xyz", false),
            };

            foreach (var (text, searchTerm, expected) in tests)
            {
                var result = StringNormalizationHelper.ContainsIgnoreCaseAndAccents(text, searchTerm);
                bool pass = result == expected;
                Console.WriteLine($"{(pass ? "✓" : "✗")} ContainsIgnoreCaseAndAccents('{text}', '{searchTerm}') = {result} (esperado: {expected})");
            }
        }

        public static void RunAllTests()
        {
            TestRemoveAccents();
            TestContainsIgnoreCaseAndAccents();
            Console.WriteLine("\n=== TESTES CONCLUÍDOS ===");
        }
    }
}
