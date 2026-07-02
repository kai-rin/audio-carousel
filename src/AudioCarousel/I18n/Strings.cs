namespace AudioCarousel.I18n;

public static class Strings
{
    private static Language _current = Language.English;

    private static Dictionary<Language, string> M(
        string en, string ja, string zhHans, string zhHant,
        string es, string fr, string de, string ptBr, string ru, string ko) => new()
        {
            [Language.English] = en,
            [Language.Japanese] = ja,
            [Language.ChineseSimplified] = zhHans,
            [Language.ChineseTraditional] = zhHant,
            [Language.Spanish] = es,
            [Language.French] = fr,
            [Language.German] = de,
            [Language.PortugueseBrazil] = ptBr,
            [Language.Russian] = ru,
            [Language.Korean] = ko,
        };

    // Same value for every language — used for proper nouns and language self-names.
    private static Dictionary<Language, string> Same(string s) => new()
    {
        [Language.English] = s,
        [Language.Japanese] = s,
        [Language.ChineseSimplified] = s,
        [Language.ChineseTraditional] = s,
        [Language.Spanish] = s,
        [Language.French] = s,
        [Language.German] = s,
        [Language.PortugueseBrazil] = s,
        [Language.Russian] = s,
        [Language.Korean] = s,
    };

    private static readonly Dictionary<string, Dictionary<Language, string>> Table = new()
    {
        ["app.title"] = Same("Audio Carousel"),
        ["tray.title"] = Same("Audio Carousel"),

        ["tray.currentPrefix"] = M(
            "Current: ", "現在: ",
            "当前: ", "目前: ",
            "Actual: ", "Actuel : ",
            "Aktuell: ", "Atual: ",
            "Текущее: ", "현재: "),
        ["tray.currentNone"] = M(
            "(no device selected)", "(デバイス未選択)",
            "(未选择设备)", "(未選擇裝置)",
            "(ningún dispositivo seleccionado)", "(aucun périphérique sélectionné)",
            "(kein Gerät ausgewählt)", "(nenhum dispositivo selecionado)",
            "(устройство не выбрано)", "(장치가 선택되지 않음)"),
        ["tray.cycleNext"] = M(
            "Cycle next", "次のデバイスへ",
            "切换到下一个设备", "切換到下一個裝置",
            "Siguiente dispositivo", "Périphérique suivant",
            "Nächstes Gerät", "Próximo dispositivo",
            "Следующее устройство", "다음 장치"),
        ["tray.settings"] = M(
            "Settings...", "設定...",
            "设置...", "設定...",
            "Configuración...", "Paramètres...",
            "Einstellungen...", "Configurações...",
            "Настройки...", "설정..."),
        ["tray.startWithWindows"] = M(
            "Start with Windows", "Windows起動時に開始",
            "随 Windows 启动", "隨 Windows 啟動",
            "Iniciar con Windows", "Démarrer avec Windows",
            "Mit Windows starten", "Iniciar com o Windows",
            "Запускать с Windows", "Windows 시작 시 실행"),
        ["tray.about"] = M(
            "About", "バージョン情報",
            "关于", "關於",
            "Acerca de", "À propos",
            "Info", "Sobre",
            "О программе", "정보"),
        ["tray.exit"] = M(
            "Exit", "終了",
            "退出", "結束",
            "Salir", "Quitter",
            "Beenden", "Sair",
            "Выход", "종료"),

        ["settings.title"] = M(
            "Audio Carousel — Settings", "Audio Carousel — 設定",
            "Audio Carousel — 设置", "Audio Carousel — 設定",
            "Audio Carousel — Configuración", "Audio Carousel — Paramètres",
            "Audio Carousel — Einstellungen", "Audio Carousel — Configurações",
            "Audio Carousel — Настройки", "Audio Carousel — 설정"),
        ["settings.titleFirstRun"] = M(
            "Audio Carousel — Settings (First-time setup)", "Audio Carousel — 設定 (初回セットアップ)",
            "Audio Carousel — 设置（首次设置）", "Audio Carousel — 設定（首次設定）",
            "Audio Carousel — Configuración (configuración inicial)", "Audio Carousel — Paramètres (configuration initiale)",
            "Audio Carousel — Einstellungen (Ersteinrichtung)", "Audio Carousel — Configurações (configuração inicial)",
            "Audio Carousel — Настройки (первоначальная настройка)", "Audio Carousel — 설정 (초기 설정)"),
        ["settings.hotkey"] = M(
            "Hotkey:", "ホットキー:",
            "热键:", "快速鍵:",
            "Tecla rápida:", "Raccourci :",
            "Tastenkombination:", "Tecla de atalho:",
            "Сочетание клавиш:", "단축키:"),
        ["settings.hotkeyHint"] = M(
            "(Click and press a key combination)", "(クリックしてキーを押してください)",
            "（点击后按下组合键）", "（點擊後按下組合鍵）",
            "(Haga clic y pulse una combinación de teclas)", "(Cliquez et appuyez sur une combinaison de touches)",
            "(Klicken und Tastenkombination drücken)", "(Clique e pressione uma combinação de teclas)",
            "(Нажмите и введите сочетание клавиш)", "(클릭 후 키 조합을 누르세요)"),
        ["settings.hotkeyCapturing"] = M(
            "Press a key combination... (Esc cancels)", "キー組み合わせを押してください... (Escでキャンセル)",
            "按下组合键...（Esc 取消）", "按下組合鍵...（Esc 取消）",
            "Pulse una combinación de teclas... (Esc cancela)", "Appuyez sur une combinaison de touches... (Échap annule)",
            "Tastenkombination drücken... (Esc bricht ab)", "Pressione uma combinação de teclas... (Esc cancela)",
            "Нажмите сочетание клавиш... (Esc — отмена)", "키 조합을 누르세요... (Esc로 취소)"),
        ["settings.hotkeyClear"] = M(
            "Clear", "クリア",
            "清除", "清除",
            "Borrar", "Effacer",
            "Löschen", "Limpar",
            "Очистить", "지우기"),
        ["settings.hotkeyEmpty"] = M(
            "(none)", "(未設定)",
            "（未设置）", "（未設定）",
            "(ninguna)", "(aucun)",
            "(keine)", "(nenhuma)",
            "(нет)", "(없음)"),
        ["settings.cycleDevices"] = M(
            "Cycle devices (in order):", "切替デバイス (順序):",
            "切换设备（按顺序）:", "切換裝置（依順序）:",
            "Dispositivos en ciclo (en orden):", "Périphériques à parcourir (dans l'ordre) :",
            "Geräte im Wechsel (in Reihenfolge):", "Dispositivos no ciclo (em ordem):",
            "Устройства в цикле (по порядку):", "순환 장치 (순서대로):"),
        ["settings.addDevice"] = M(
            "Add device", "デバイス追加",
            "添加设备", "新增裝置",
            "Agregar dispositivo", "Ajouter un périphérique",
            "Gerät hinzufügen", "Adicionar dispositivo",
            "Добавить устройство", "장치 추가"),
        ["settings.remove"] = M(
            "Remove", "削除",
            "移除", "移除",
            "Quitar", "Supprimer",
            "Entfernen", "Remover",
            "Удалить", "제거"),
        ["settings.moveUp"] = M(
            "Up", "上へ",
            "上移", "上移",
            "Arriba", "Monter",
            "Nach oben", "Para cima",
            "Вверх", "위로"),
        ["settings.moveDown"] = M(
            "Down", "下へ",
            "下移", "下移",
            "Abajo", "Descendre",
            "Nach unten", "Para baixo",
            "Вниз", "아래로"),
        ["settings.language"] = M(
            "Language:", "言語:",
            "语言:", "語言:",
            "Idioma:", "Langue :",
            "Sprache:", "Idioma:",
            "Язык:", "언어:"),
        ["settings.languageAuto"] = M(
            "Auto", "自動",
            "自动", "自動",
            "Automático", "Automatique",
            "Automatisch", "Automático",
            "Авто", "자동"),

        // Language self-names — same value across all languages so users can find their language.
        ["settings.languageEn"] = Same("English"),
        ["settings.languageJa"] = Same("日本語"),
        ["settings.languageZhHans"] = Same("简体中文"),
        ["settings.languageZhHant"] = Same("繁體中文"),
        ["settings.languageEs"] = Same("Español"),
        ["settings.languageFr"] = Same("Français"),
        ["settings.languageDe"] = Same("Deutsch"),
        ["settings.languagePtBr"] = Same("Português (Brasil)"),
        ["settings.languageRu"] = Same("Русский"),
        ["settings.languageKo"] = Same("한국어"),

        ["settings.startWithWindows"] = M(
            "Start with Windows", "Windows起動時に開始",
            "随 Windows 启动", "隨 Windows 啟動",
            "Iniciar con Windows", "Démarrer avec Windows",
            "Mit Windows starten", "Iniciar com o Windows",
            "Запускать с Windows", "Windows 시작 시 실행"),
        ["settings.ok"] = Same("OK"),
        ["settings.cancel"] = M(
            "Cancel", "キャンセル",
            "取消", "取消",
            "Cancelar", "Annuler",
            "Abbrechen", "Cancelar",
            "Отмена", "취소"),
        ["settings.offline"] = M(
            "(offline)", "(未接続)",
            "（离线）", "（離線）",
            "(desconectado)", "(hors ligne)",
            "(offline)", "(offline)",
            "(не в сети)", "(오프라인)"),
        ["settings.noNewDevices"] = M(
            "(no new devices available)", "(追加可能なデバイスがありません)",
            "（没有可添加的新设备）", "（沒有可新增的新裝置）",
            "(no hay dispositivos nuevos disponibles)", "(aucun nouveau périphérique disponible)",
            "(keine neuen Geräte verfügbar)", "(nenhum dispositivo novo disponível)",
            "(новых устройств нет)", "(추가 가능한 새 장치가 없습니다)"),

        ["error.alreadyRunning"] = M(
            "Audio Carousel is already running.", "Audio Carouselはすでに起動しています。",
            "Audio Carousel 已在运行。", "Audio Carousel 已在執行中。",
            "Audio Carousel ya se está ejecutando.", "Audio Carousel est déjà en cours d'exécution.",
            "Audio Carousel wird bereits ausgeführt.", "Audio Carousel já está em execução.",
            "Audio Carousel уже запущен.", "Audio Carousel이 이미 실행 중입니다."),
        ["error.hotkeyInUse"] = M(
            "Hotkey already in use by another application.", "このホットキーは他のアプリに使用されています。",
            "热键已被其他应用程序占用。", "快速鍵已被其他應用程式佔用。",
            "La tecla rápida ya está en uso por otra aplicación.", "Le raccourci est déjà utilisé par une autre application.",
            "Die Tastenkombination wird bereits von einer anderen Anwendung verwendet.", "A tecla de atalho já está em uso por outro aplicativo.",
            "Сочетание клавиш уже используется другим приложением.", "다른 응용 프로그램이 이 단축키를 이미 사용하고 있습니다."),
        ["error.switchFailed"] = M(
            "Failed to switch device", "デバイス切替に失敗しました",
            "切换设备失败", "切換裝置失敗",
            "Error al cambiar de dispositivo", "Échec du changement de périphérique",
            "Gerätewechsel fehlgeschlagen", "Falha ao alternar dispositivo",
            "Не удалось переключить устройство", "장치 전환에 실패했습니다"),
        ["error.noDeviceAvailable"] = M(
            "No registered audio device available", "切替可能なデバイスがありません",
            "没有可用的已注册音频设备", "沒有可用的已註冊音訊裝置",
            "No hay dispositivos de audio registrados disponibles", "Aucun périphérique audio enregistré disponible",
            "Kein registriertes Audiogerät verfügbar", "Nenhum dispositivo de áudio registrado disponível",
            "Нет доступных зарегистрированных аудиоустройств", "사용 가능한 등록된 오디오 장치가 없습니다"),
        ["error.configCorrupted"] = M(
            "Configuration file was corrupted. A backup was saved as audio-carousel.json.bak and defaults are now in use.",
            "設定ファイルが破損していました。audio-carousel.json.bakにバックアップを保存し、デフォルト設定で起動します。",
            "配置文件已损坏。已将备份保存为 audio-carousel.json.bak，现在使用默认设置。",
            "設定檔已損毀。已將備份儲存為 audio-carousel.json.bak，現在使用預設設定。",
            "El archivo de configuración estaba dañado. Se guardó una copia de seguridad como audio-carousel.json.bak y ahora se utilizan los valores predeterminados.",
            "Le fichier de configuration était corrompu. Une sauvegarde a été enregistrée sous audio-carousel.json.bak et les valeurs par défaut sont désormais utilisées.",
            "Die Konfigurationsdatei war beschädigt. Eine Sicherung wurde als audio-carousel.json.bak gespeichert und die Standardwerte werden nun verwendet.",
            "O arquivo de configuração estava corrompido. Foi salvo um backup como audio-carousel.json.bak e os padrões agora estão em uso.",
            "Файл конфигурации был повреждён. Резервная копия сохранена как audio-carousel.json.bak, теперь используются настройки по умолчанию.",
            "구성 파일이 손상되었습니다. 백업이 audio-carousel.json.bak으로 저장되었으며 이제 기본값이 사용됩니다."),
        ["error.unhandled"] = M(
            "An unexpected error occurred:", "予期しないエラーが発生しました:",
            "发生意外错误:", "發生未預期的錯誤:",
            "Se produjo un error inesperado:", "Une erreur inattendue s'est produite :",
            "Ein unerwarteter Fehler ist aufgetreten:", "Ocorreu um erro inesperado:",
            "Произошла непредвиденная ошибка:", "예기치 못한 오류가 발생했습니다:"),

        ["about.body"] = M(
            "Audio Carousel — switch the default audio output device with a global hotkey.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — グローバルホットキーで音声出力デバイスを切り替えます。\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — 通过全局热键切换默认音频输出设备。\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — 透過全域快速鍵切換預設音訊輸出裝置。\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — cambia el dispositivo de salida de audio predeterminado con una tecla rápida global.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — change le périphérique de sortie audio par défaut avec un raccourci global.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — wechselt mit einer globalen Tastenkombination das Standard-Audiowiedergabegerät.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — alterna o dispositivo de saída de áudio padrão com uma tecla de atalho global.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — переключает аудиоустройство по умолчанию глобальным сочетанием клавиш.\n\nhttps://github.com/kai-rin/audio-carousel",
            "Audio Carousel — 전역 단축키로 기본 오디오 출력 장치를 전환합니다.\n\nhttps://github.com/kai-rin/audio-carousel"),
    };

    public static void SetLanguage(Language lang) => _current = lang;

    public static string Get(string key)
    {
        if (!Table.TryGetValue(key, out var entry)) return key;
        if (entry.TryGetValue(_current, out var s)) return s;
        return entry.TryGetValue(Language.English, out var en) ? en : key;
    }

    public static Language ResolveLanguage(string configValue, string currentUiCultureName)
    {
        return configValue switch
        {
            "en" => Language.English,
            "ja" => Language.Japanese,
            "zh-Hans" or "zh-CN" or "zh-SG" => Language.ChineseSimplified,
            "zh-Hant" or "zh-TW" or "zh-HK" or "zh-MO" => Language.ChineseTraditional,
            "es" => Language.Spanish,
            "fr" => Language.French,
            "de" => Language.German,
            "pt-BR" or "pt" or "pt-PT" => Language.PortugueseBrazil,
            "ru" => Language.Russian,
            "ko" => Language.Korean,
            _ => DetectFromCulture(currentUiCultureName),
        };
    }

    private static Language DetectFromCulture(string name)
    {
        if (string.IsNullOrEmpty(name)) return Language.English;

        if (name.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return Language.Japanese;

        if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            bool isTraditional = name.Contains("Hant", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase);
            return isTraditional ? Language.ChineseTraditional : Language.ChineseSimplified;
        }

        if (name.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return Language.Spanish;
        if (name.StartsWith("fr", StringComparison.OrdinalIgnoreCase)) return Language.French;
        if (name.StartsWith("de", StringComparison.OrdinalIgnoreCase)) return Language.German;
        if (name.StartsWith("pt", StringComparison.OrdinalIgnoreCase)) return Language.PortugueseBrazil;
        if (name.StartsWith("ru", StringComparison.OrdinalIgnoreCase)) return Language.Russian;
        if (name.StartsWith("ko", StringComparison.OrdinalIgnoreCase)) return Language.Korean;

        return Language.English;
    }

    public static string GetCurrentUiCultureName() =>
        System.Globalization.CultureInfo.CurrentUICulture.Name;
}
