using System;
using System.Collections.Generic;

namespace RSPP
{
    /// <summary>The four languages the player can choose on the entrance screen.</summary>
    public enum Language
    {
        English,
        Spanish,
        Turkish,
        French
    }

    /// <summary>
    /// All on-screen sentences in one table: English, Spanish, Turkish, French (in that order).
    /// Use Loc.T("key") for a sentence and Loc.F("key", values...) for a sentence with {0}, {1} ... placeholders.
    /// To change a sentence or fix a translation, edit the table below. Plain C#, no Unity.
    /// </summary>
    public static class Loc
    {
        public static Language Current { get; private set; }

        /// <summary>Raised after the language changes, so fixed labels can update themselves.</summary>
        public static event Action Changed;

        public static void Set(Language language)
        {
            Current = language;
            if (Changed != null) Changed();
        }

        /// <summary>Forgets all listeners (called once when the game is built).</summary>
        public static void ClearListeners()
        {
            Changed = null;
        }

        public static string T(string key)
        {
            string[] row;
            if (Table.TryGetValue(key, out row)) return row[(int)Current];
            return key;     // a missing key shows itself, which makes it easy to spot
        }

        public static string F(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        /// <summary>The language's own name, shown on its button.</summary>
        public static string NativeName(Language language)
        {
            switch (language)
            {
                case Language.Spanish: return "Español";
                case Language.Turkish: return "Türkçe";
                case Language.French: return "Français";
                default: return "English";
            }
        }

        //                                     English, Spanish, Turkish, French
        private static readonly Dictionary<string, string[]> Table = new Dictionary<string, string[]>
        {
            // ---------------------------------------------------------------- Entrance screen
            { "welcome", new[] { "Welcome!", "¡Bienvenido!", "Hoş geldin!", "Bienvenue !" } },
            { "choose_lang", new[] { "Choose your language", "Elige tu idioma", "Dilini seç", "Choisis ta langue" } },
            { "enter", new[] { "Enter the classroom", "Entrar al salón", "Sınıfa gir", "Entrer dans la classe" } },
            { "instructions", new[] { "Instructions", "Instrucciones", "Nasıl oynanır", "Règles du jeu" } },
            { "back", new[] { "Back", "Atrás", "Geri", "Retour" } },
            { "class_sign", new[] { "Classroom 2B", "Salón 2B", "2-B Sınıfı", "Classe 2B" } },
            { "instructions_text", new[] {
                "1. ROLL: Grab the die with your LEFT hand. Let go to roll it.\n2. Your game piece moves by itself.\n3. SPIN: Hold the SPIN handle with your LEFT hand and press the trigger.\n4. ANSWER: Point with your RIGHT hand and press the trigger.\n5. Sometimes {0} has an idea. Touch {0}, then check if {0} is right!\nCollect stars and reach the FINISH!",
                "1. LANZA: Toma el dado con tu mano IZQUIERDA. Suéltalo para lanzarlo.\n2. Tu ficha avanza sola.\n3. GIRA: Sujeta la palanca SPIN con tu mano IZQUIERDA y aprieta el gatillo.\n4. RESPONDE: Apunta con tu mano DERECHA y aprieta el gatillo.\n5. A veces {0} tiene una idea. ¡Toca a {0} y revisa si tiene razón!\n¡Junta estrellas y llega a la META!",
                "1. AT: Zarı SOL elinle tut. Bırakınca zar atılır.\n2. Piyonun kendi kendine ilerler.\n3. ÇEVİR: SPIN kolunu SOL elinle tut ve tetiğe bas.\n4. CEVAPLA: SAĞ elinle işaret et ve tetiğe bas.\n5. {0}'nun bazen bir fikri olur. {0}'ya dokun, sonra haklı mı kontrol et!\nYıldız topla ve BİTİŞ'e ulaş!",
                "1. LANCE : Prends le dé avec ta main GAUCHE. Lâche-le pour le lancer.\n2. Ton pion avance tout seul.\n3. TOURNE : Tiens la poignée SPIN avec ta main GAUCHE et appuie sur la gâchette.\n4. RÉPONDS : Vise avec ta main DROITE et appuie sur la gâchette.\n5. Parfois, {0} a une idée. Touche {0}, puis vérifie s'il a raison !\nGagne des étoiles et atteins l'ARRIVÉE !" } },

            // ---------------------------------------------------------------- In-game menu (right side)
            { "menu_title", new[] { "Menu", "Menú", "Menü", "Menu" } },
            { "back_to_menu", new[] { "Back to Menu", "Volver al menú", "Ana menüye dön", "Retour au menu" } },
            { "music_on", new[] { "Music: ON", "Música: SÍ", "Müzik: AÇIK", "Musique : OUI" } },
            { "music_off", new[] { "Music: OFF", "Música: NO", "Müzik: KAPALI", "Musique : NON" } },
            { "sound_on", new[] { "Sound: ON", "Sonido: SÍ", "Ses: AÇIK", "Son : OUI" } },
            { "sound_off", new[] { "Sound: OFF", "Sonido: NO", "Ses: KAPALI", "Son : NON" } },
            { "leave_q", new[] { "Leave the game?", "¿Salir del juego?", "Oyundan çıkılsın mı?", "Quitter la partie ?" } },
            { "yes", new[] { "Yes", "Sí", "Evet", "Oui" } },
            { "no", new[] { "No", "No", "Hayır", "Non" } },

            // ---------------------------------------------------------------- Turn flow
            { "your_turn_title", new[] { "Your Turn", "Tu turno", "Sıra sende", "À toi de jouer" } },
            { "your_turn_msg", new[] {
                "Roll the die!\nGrab it with your LEFT hand, then let go.",
                "¡Lanza el dado!\nTómalo con tu mano IZQUIERDA y luego suéltalo.",
                "Zarı at!\nSOL elinle tut, sonra bırak.",
                "Lance le dé !\nPrends-le avec ta main GAUCHE, puis lâche-le." } },
            { "extra_roll_title", new[] { "Extra Roll!", "¡Tiro extra!", "Ekstra zar!", "Lancer bonus !" } },
            { "extra_roll_msg", new[] {
                "You won an extra roll!\nGrab the die with your LEFT hand and roll it again.",
                "¡Ganaste un tiro extra!\nToma el dado con tu mano IZQUIERDA y lánzalo otra vez.",
                "Ekstra zar kazandın!\nZarı SOL elinle tut ve tekrar at.",
                "Tu as gagné un lancer bonus !\nPrends le dé avec ta main GAUCHE et lance-le encore." } },
            { "rolling", new[] { "Rolling...", "Lanzando...", "Zar atılıyor...", "Le dé roule..." } },
            { "rolled_title", new[] { "You rolled a {0}!", "¡Sacaste un {0}!", "{0} attın!", "Tu as fait {0} !" } },
            { "rolled_one", new[] {
                "Watch your game piece move 1 space.", "Mira cómo tu ficha avanza 1 casilla.",
                "Piyonunun 1 kare ilerlemesini izle.", "Regarde ton pion avancer d'1 case." } },
            { "rolled_many", new[] {
                "Watch your game piece move {0} spaces.", "Mira cómo tu ficha avanza {0} casillas.",
                "Piyonunun {0} kare ilerlemesini izle.", "Regarde ton pion avancer de {0} cases." } },
            { "new_part", new[] {
                "You reached a new part of the board! Bigger numbers can come now.",
                "¡Llegaste a una nueva parte del tablero! Ahora pueden venir números más grandes.",
                "Tahtanın yeni bir bölümüne geldin! Artık daha büyük sayılar gelebilir.",
                "Tu arrives dans une nouvelle partie du plateau ! De plus grands nombres peuvent arriver." } },
            { "spin_title", new[] { "Spin the Wheel", "Gira la ruleta", "Çarkı çevir", "Fais tourner la roue" } },
            { "spin_msg", new[] {
                "Hold the SPIN handle with your LEFT hand and press the trigger.",
                "Sujeta la palanca SPIN con tu mano IZQUIERDA y aprieta el gatillo.",
                "SPIN kolunu SOL elinle tut ve tetiğe bas.",
                "Tiens la poignée SPIN avec ta main GAUCHE et appuie sur la gâchette." } },
            { "spinning", new[] { "Spinning...", "Girando...", "Çark dönüyor...", "La roue tourne..." } },
            { "bonus_title", new[] { "Bonus!", "¡Bonus!", "Bonus!", "Bonus !" } },
            { "bonus_msg", new[] {
                "Lucky spin! You get a free star!", "¡Qué suerte! ¡Ganas una estrella gratis!",
                "Şanslı çevirme! Bedava bir yıldız kazandın!", "Quelle chance ! Tu gagnes une étoile !" } },
            { "next", new[] { "Next", "Siguiente", "İleri", "Suivant" } },
            { "go", new[] { "Go!", "¡Vamos!", "Başla!", "C'est parti !" } },
            { "play_again", new[] { "Play Again", "Jugar otra vez", "Tekrar oyna", "Rejouer" } },

            // ---------------------------------------------------------------- Questions and feedback
            { "cat_add", new[] { "Addition", "Suma", "Toplama", "Addition" } },
            { "cat_sub", new[] { "Subtraction", "Resta", "Çıkarma", "Soustraction" } },
            { "cat_oddeven", new[] { "Odd or Even", "Par o impar", "Tek mi çift mi", "Pair ou impair" } },
            { "level", new[] { "Level {0}", "Nivel {0}", "Seviye {0}", "Niveau {0}" } },
            { "stars", new[] { "Stars: {0}", "Estrellas: {0}", "Yıldız: {0}", "Étoiles : {0}" } },
            { "correct_mini", new[] { "Correct! Great job!", "¡Correcto! ¡Muy bien!", "Doğru! Harika!", "Juste ! Bravo !" } },
            { "correct_star", new[] {
                "Correct! You earned a star!", "¡Correcto! ¡Ganaste una estrella!",
                "Doğru! Bir yıldız kazandın!", "Juste ! Tu gagnes une étoile !" } },
            { "not_quite", new[] {
                "Not quite. {0} Try the next question!", "Casi. {0} ¡Intenta la próxima pregunta!",
                "Olmadı. {0} Sıradaki soruyu dene!", "Presque. {0} Essaie la question suivante !" } },
            { "level_up", new[] {
                " Level up! Now Level {0}.", " ¡Subes de nivel! Ahora nivel {0}.",
                " Seviye atladın! Şimdi Seviye {0}.", " Niveau supérieur ! Maintenant niveau {0}." } },
            { "level_down", new[] {
                " Let's practice with smaller numbers. Now Level {0}.",
                " Practiquemos con números más pequeños. Ahora nivel {0}.",
                " Biraz daha küçük sayılarla çalışalım. Şimdi Seviye {0}.",
                " Entraînons-nous avec des nombres plus petits. Maintenant niveau {0}." } },
            { "exp_add", new[] { "{0} plus {1} equals {2}.", "{0} más {1} es igual a {2}.", "{0} artı {1} eşittir {2}.", "{0} plus {1} égale {2}." } },
            { "exp_sub", new[] { "{0} minus {1} equals {2}.", "{0} menos {1} es igual a {2}.", "{0} eksi {1} eşittir {2}.", "{0} moins {1} égale {2}." } },
            { "q_is_oddeven", new[] { "Is {0} odd or even?", "¿{0} es par o impar?", "{0} tek mi, çift mi?", "{0} est-il pair ou impair ?" } },
            { "q_which_even", new[] { "Which number is even?", "¿Qué número es par?", "Hangi sayı çift?", "Quel nombre est pair ?" } },
            { "q_which_odd", new[] { "Which number is odd?", "¿Qué número es impar?", "Hangi sayı tek?", "Quel nombre est impair ?" } },
            { "odd", new[] { "Odd", "Impar", "Tek", "Impair" } },
            { "even", new[] { "Even", "Par", "Çift", "Pair" } },
            { "exp_even", new[] {
                "{0} is even. Every dot has a partner.", "{0} es par. Cada punto tiene pareja.",
                "{0} çifttir. Her noktanın bir eşi var.", "{0} est pair. Chaque point a un partenaire." } },
            { "exp_odd", new[] {
                "{0} is odd. One dot has no partner.", "{0} es impar. Un punto se queda sin pareja.",
                "{0} tektir. Bir noktanın eşi yok.", "{0} est impair. Un point n'a pas de partenaire." } },
            { "visual_even", new[] { "{0} is even", "{0} es par", "{0} çift", "{0} est pair" } },
            { "visual_odd", new[] { "{0} is odd", "{0} es impar", "{0} tek", "{0} est impair" } },
            { "listen", new[] { "Listen", "Escuchar", "Dinle", "Écouter" } },

            // ---------------------------------------------------------------- Robo, the simulated AI Helper
            { "robo_name", new[] { "{0} the AI Helper", "{0}, ayudante de IA", "Yapay zekâ yardımcısı {0}", "{0}, l'assistant IA" } },
            { "robo_idle", new[] {
                "Hi! I am {0}, your AI Helper. I sometimes make mistakes. Can you help me check my answers?",
                "¡Hola! Soy {0}, tu ayudante de IA. A veces me equivoco. ¿Me ayudas a revisar mis respuestas?",
                "Merhaba! Ben {0}, senin yapay zekâ yardımcın. Bazen hata yaparım. Cevaplarımı kontrol etmeme yardım eder misin?",
                "Salut ! Je suis {0}, ton assistant IA. Je fais parfois des erreurs. Tu peux m'aider à vérifier mes réponses ?" } },
            { "robo_touch", new[] {
                "I have an idea! Touch me with your LEFT hand.", "¡Tengo una idea! Tócame con tu mano IZQUIERDA.",
                "Bir fikrim var! Bana SOL elinle dokun.", "J'ai une idée ! Touche-moi avec ta main GAUCHE." } },
            { "robo_idea", new[] {
                "{0} has an idea! Touch {0} with your LEFT hand.", "¡{0} tiene una idea! Toca a {0} con tu mano IZQUIERDA.",
                "{0}'nun bir fikri var! {0}'ya SOL elinle dokun.", "{0} a une idée ! Touche {0} avec ta main GAUCHE." } },
            { "robo_check", new[] {
                "Can you help {0} check it? Press Correct or Incorrect.", "¿Ayudas a {0} a revisarlo? Pulsa Correcto o Incorrecto.",
                "{0}'nun cevabını kontrol eder misin? Doğru ya da Yanlış'a bas.", "Tu peux aider {0} à vérifier ? Appuie sur Juste ou Faux." } },
            { "robo_ask", new[] { "Is {0} right?", "¿{0} tiene razón?", "{0} haklı mı?", "{0} a-t-il raison ?" } },
            { "correct_btn", new[] { "Correct", "Correcto", "Doğru", "Juste" } },
            { "incorrect_btn", new[] { "Incorrect", "Incorrecto", "Yanlış", "Faux" } },
            { "suggest", new[] { "I think the answer is {0}!", "¡Creo que la respuesta es {0}!", "Bence cevap {0}!", "Je pense que la réponse est {0} !" } },
            { "helper_right_good", new[] {
                "Yes! {0} was right. {1} You earned a star!", "¡Sí! {0} tenía razón. {1} ¡Ganaste una estrella!",
                "Evet! {0} haklıydı. {1} Bir yıldız kazandın!", "Oui ! {0} avait raison. {1} Tu gagnes une étoile !" } },
            { "helper_wrong_good", new[] {
                "Great checking! You helped {0} fix a mistake. {1} You earned a star!",
                "¡Muy bien revisado! Ayudaste a {0} a corregir un error. {1} ¡Ganaste una estrella!",
                "Harika kontrol! {0}'nun hatasını düzeltmesine yardım ettin. {1} Bir yıldız kazandın!",
                "Bien vérifié ! Tu as aidé {0} à corriger une erreur. {1} Tu gagnes une étoile !" } },
            { "helper_right_bad", new[] {
                "{0} was right this time. {1} Checking is always a good idea!",
                "Esta vez {0} tenía razón. {1} ¡Revisar siempre es buena idea!",
                "{0} bu sefer haklıydı. {1} Kontrol etmek her zaman iyi bir fikir!",
                "Cette fois, {0} avait raison. {1} Vérifier, c'est toujours une bonne idée !" } },
            { "helper_wrong_bad", new[] {
                "{0} made a small mistake. {1} Let's always check AI answers together!",
                "{0} cometió un pequeño error. {1} ¡Revisemos siempre juntos las respuestas de la IA!",
                "{0} küçük bir hata yaptı. {1} Yapay zekânın cevaplarını hep birlikte kontrol edelim!",
                "{0} a fait une petite erreur. {1} Vérifions toujours ensemble les réponses de l'IA !" } },
            { "robo_was_right", new[] {
                "I was right this time! Thank you for checking.", "¡Esta vez tenía razón! Gracias por revisar.",
                "Bu sefer haklıydım! Kontrol ettiğin için teşekkürler.", "Cette fois, j'avais raison ! Merci d'avoir vérifié." } },
            { "robo_was_wrong", new[] {
                "Oops, I made a mistake. Thank you for helping me!", "¡Uy, me equivoqué! ¡Gracias por ayudarme!",
                "Hay aksi, hata yaptım. Yardım ettiğin için teşekkürler!", "Oups, je me suis trompé. Merci de m'avoir aidé !" } },
            { "robo_new_part", new[] {
                "You reached a new part of the board! Bigger numbers can come now.",
                "¡Llegaste a una nueva parte del tablero! Ahora pueden venir números más grandes.",
                "Tahtanın yeni bir bölümüne geldin! Artık daha büyük sayılar gelebilir.",
                "Tu arrives dans une nouvelle partie du plateau ! De plus grands nombres peuvent arriver." } },
            { "robo_done", new[] { "You did it! Great job!", "¡Lo lograste! ¡Muy bien!", "Başardın! Harika!", "Tu as réussi ! Bravo !" } },

            // ---------------------------------------------------------------- Mini challenge and finish
            { "mini_title", new[] { "Mini Challenge", "Minidesafío", "Mini Görev", "Mini défi" } },
            { "mini_intro", new[] {
                "{0}\nAnswer {1} quick questions. Get {2} right to win a star. Get all {3} right to win an extra roll too!",
                "{0}\nResponde {1} preguntas rápidas. Acierta {2} para ganar una estrella. ¡Acierta las {3} para ganar también un tiro extra!",
                "{0}\n{1} kısa soruyu cevapla. {2} doğru yaparsan bir yıldız kazanırsın. Hepsini doğru yaparsan ekstra zar da kazanırsın!",
                "{0}\nRéponds à {1} questions rapides. {2} bonnes réponses = une étoile. Les {3} bonnes = une étoile et un lancer bonus !" } },
            { "mini_landed", new[] { "You landed on a star space!", "¡Caíste en una casilla de estrella!", "Yıldızlı bir kareye geldin!", "Tu es sur une case étoile !" } },
            { "mini_wheel", new[] { "The wheel picked a Mini Challenge!", "¡La ruleta eligió un minidesafío!", "Çark bir Mini Görev seçti!", "La roue a choisi un mini défi !" } },
            { "mini_q", new[] { "Mini Challenge  {0} of {1}", "Minidesafío  {0} de {1}", "Mini Görev  {0}/{1}", "Mini défi  {0} sur {1}" } },
            { "mini_score", new[] { "You got {0} of {1} right.", "Acertaste {0} de {1}.", "{1} sorudan {0} tanesini bildin.", "Tu as {0} bonnes réponses sur {1}." } },
            { "mini_perfect", new[] {
                "Perfect! You win a star AND an extra roll!", "¡Perfecto! ¡Ganas una estrella Y un tiro extra!",
                "Mükemmel! Bir yıldız VE ekstra zar kazandın!", "Parfait ! Tu gagnes une étoile ET un lancer bonus !" } },
            { "mini_star", new[] { "Well done! You win a star!", "¡Bien hecho! ¡Ganas una estrella!", "Aferin! Bir yıldız kazandın!", "Bien joué ! Tu gagnes une étoile !" } },
            { "mini_try", new[] { "Nice try! Keep practicing.", "¡Buen intento! Sigue practicando.", "Güzel deneme! Çalışmaya devam.", "Bel essai ! Continue à t'entraîner." } },
            { "finish_title", new[] { "FINISH!", "¡META!", "BİTİŞ!", "ARRIVÉE !" } },
            { "finish_summary", new[] {
                "Stars: {0}\nMath: {1} of {2} right\nYou helped {3}: {4} of {5} checks right\nHighest level: {6}",
                "Estrellas: {0}\nMatemáticas: {1} de {2} correctas\nAyudaste a {3}: {4} de {5} revisiones bien\nNivel más alto: {6}",
                "Yıldız: {0}\nMatematik: {2} sorudan {1} doğru\n{3}'ya yardım: {5} kontrolden {4} doğru\nEn yüksek seviye: {6}",
                "Étoiles : {0}\nMaths : {1} sur {2} justes\nTu as aidé {3} : {4} sur {5} vérifications justes\nNiveau le plus haut : {6}" } },

            // ---------------------------------------------------------------- Board, wheel, classroom
            { "board_start", new[] { "START", "SALIDA", "BAŞLA", "DÉPART" } },
            { "board_finish", new[] { "FINISH", "META", "BİTİŞ", "ARRIVÉE" } },
            { "board_roll", new[] { "ROLL", "LANZA", "AT", "LANCE" } },
            { "board_spin", new[] { "SPIN", "SPIN", "SPIN", "SPIN" } },
            { "wheel_add", new[] { "+\nAdd", "+\nSuma", "+\nTopla", "+\nAjoute" } },
            { "wheel_sub", new[] { "-\nTake away", "-\nQuita", "-\nÇıkar", "-\nEnlève" } },
            { "wheel_oddeven", new[] { "ODD\nEVEN", "PAR\nIMPAR", "TEK\nÇİFT", "PAIR\nIMPAIR" } },
            { "wheel_mini", new[] { "MINI", "MINI", "MINI", "MINI" } },
            { "wheel_bonus", new[] { "BONUS", "BONUS", "BONUS", "BONUS" } },
            { "chalk_subtitle", new[] { "A VR Math Game", "Un juego de matemáticas en RV", "Bir VR matematik oyunu", "Un jeu de maths en RV" } },
            { "poster_numberline", new[] { "Number Line", "Recta numérica", "Sayı doğrusu", "Droite numérique" } },
            { "poster_oddeven", new[] { "Odd and Even", "Par e impar", "Tek ve çift", "Pair et impair" } },
            { "poster_even", new[] { "EVEN: 0  2  4  6  8", "PAR: 0  2  4  6  8", "ÇİFT: 0  2  4  6  8", "PAIR : 0  2  4  6  8" } },
            { "poster_odd", new[] { "ODD: 1  3  5  7  9", "IMPAR: 1  3  5  7  9", "TEK: 1  3  5  7  9", "IMPAIR : 1  3  5  7  9" } },
        };
    }
}
