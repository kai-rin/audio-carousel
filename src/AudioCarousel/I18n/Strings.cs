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

        // Shared between the tray menu and the settings dialog.
        ["common.startWithWindows"] = M(
            "Start with Windows", "Windows起動時に開始",
            "随 Windows 启动", "隨 Windows 啟動",
            "Iniciar con Windows", "Démarrer avec Windows",
            "Mit Windows starten", "Iniciar com o Windows",
            "Запускать с Windows", "Windows 시작 시 실행"),
        ["common.offline"] = M(
            "(offline)", "(未接続)",
            "（离线）", "（離線）",
            "(desconectado)", "(hors ligne)",
            "(nicht verbunden)", "(desconectado)",
            "(не в сети)", "(오프라인)"),

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
        ["tray.cyclePrevious"] = M(
            "Previous device",
            "前のデバイスへ",
            "切换到上一个设备",
            "切換到上一個裝置",
            "Dispositivo anterior",
            "Périphérique précédent",
            "Vorheriges Gerät",
            "Dispositivo anterior",
            "Предыдущее устройство",
            "이전 장치"),
        ["tray.settings"] = M(
            "Settings...", "設定...",
            "设置...", "設定...",
            "Configuración...", "Paramètres...",
            "Einstellungen...", "Configurações...",
            "Настройки...", "설정..."),
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
        ["settings.hotkeys"] = M(
            "Hotkeys",
            "ホットキー",
            "热键",
            "快速鍵",
            "Teclas rápidas",
            "Raccourcis",
            "Tastenkombinationen",
            "Teclas de atalho",
            "Сочетания клавиш",
            "단축키"),
        ["settings.hotkeyNext"] = M(
            "Next device:",
            "次のデバイス:",
            "下一个设备:",
            "下一個裝置:",
            "Siguiente dispositivo:",
            "Périphérique suivant :",
            "Nächstes Gerät:",
            "Próximo dispositivo:",
            "Следующее устройство:",
            "다음 장치:"),
        ["settings.hotkeyPrevious"] = M(
            "Previous device:",
            "前のデバイス:",
            "上一个设备:",
            "上一個裝置:",
            "Dispositivo anterior:",
            "Périphérique précédent :",
            "Vorheriges Gerät:",
            "Dispositivo anterior:",
            "Предыдущее устройство:",
            "이전 장치:"),
        ["settings.showToast"] = M(
            "Show a notification when switching",
            "切り替え時に通知を表示する",
            "切换时显示通知",
            "切換時顯示通知",
            "Mostrar una notificación al cambiar",
            "Afficher une notification lors du changement",
            "Beim Wechseln eine Benachrichtigung anzeigen",
            "Mostrar uma notificação ao alternar",
            "Показывать уведомление при переключении",
            "전환할 때 알림 표시"),
        ["settings.leftClickCycles"] = M(
            "Left-click on the tray icon switches to the next device (off: opens Settings)",
            "トレイアイコンの左クリックで次のデバイスへ切り替える（オフ: 設定を開く）",
            "左键点击托盘图标切换到下一个设备（关闭时：打开设置）",
            "以滑鼠左鍵按一下系統匣圖示切換到下一個裝置（關閉時：開啟設定）",
            "Clic izquierdo en el icono de la bandeja: cambiar al siguiente dispositivo (desactivado: abre Configuración)",
            "Clic gauche sur l'icône : passer au périphérique suivant (désactivé : ouvre les Paramètres)",
            "Linksklick auf das Taskleistensymbol wechselt zum nächsten Gerät (aus: öffnet Einstellungen)",
            "Clique esquerdo no ícone da bandeja alterna para o próximo dispositivo (desativado: abre Configurações)",
            "Щелчок левой кнопкой по значку в трее — следующее устройство (выкл.: открыть настройки)",
            "트레이 아이콘을 왼쪽 클릭하면 다음 장치로 전환 (끄면: 설정 열기)"),
        ["settings.confirmNoDevices"] = M(
            "You haven't added any devices yet, so the hotkey won't do anything. Close Settings anyway?",
            "まだデバイスが追加されていないため、ホットキーを押しても何も起きません。このまま設定を閉じますか?",
            "尚未添加任何设备，按热键不会有任何效果。仍要关闭设置吗？",
            "尚未新增任何裝置，按快速鍵不會有任何作用。仍要關閉設定嗎？",
            "Todavía no ha agregado dispositivos, así que la tecla rápida no hará nada. ¿Cerrar Configuración de todos modos?",
            "Vous n'avez encore ajouté aucun périphérique : le raccourci ne fera rien. Fermer les Paramètres quand même ?",
            "Sie haben noch keine Geräte hinzugefügt, daher bewirkt die Tastenkombination nichts. Einstellungen trotzdem schließen?",
            "Você ainda não adicionou dispositivos, então a tecla de atalho não fará nada. Fechar as Configurações mesmo assim?",
            "Устройства ещё не добавлены, поэтому сочетание клавиш ничего не сделает. Всё равно закрыть настройки?",
            "아직 장치를 추가하지 않아 단축키를 눌러도 아무 일도 일어나지 않습니다. 그래도 설정을 닫을까요?"),
        ["settings.confirmNoHotkey"] = M(
            "No hotkey is set. You can still switch by clicking the tray icon. Continue without a hotkey?",
            "ホットキーが設定されていません。トレイアイコンのクリックでも切り替えられます。ホットキーなしで続けますか?",
            "尚未设置热键。仍可点击托盘图标进行切换。不设置热键继续吗？",
            "尚未設定快速鍵。仍可按一下系統匣圖示進行切換。不設定快速鍵繼續嗎？",
            "No hay ninguna tecla rápida. Aún puede cambiar haciendo clic en el icono de la bandeja. ¿Continuar sin tecla rápida?",
            "Aucun raccourci défini. Vous pouvez toujours changer en cliquant sur l'icône. Continuer sans raccourci ?",
            "Keine Tastenkombination festgelegt. Sie können weiterhin per Klick auf das Taskleistensymbol wechseln. Ohne Tastenkombination fortfahren?",
            "Nenhuma tecla de atalho definida. Você ainda pode alternar clicando no ícone da bandeja. Continuar sem tecla de atalho?",
            "Сочетание клавиш не задано. Переключаться можно щелчком по значку в трее. Продолжить без сочетания клавиш?",
            "단축키가 설정되지 않았습니다. 트레이 아이콘을 클릭해서도 전환할 수 있습니다. 단축키 없이 계속할까요?"),
        ["settings.hotkeyHint"] = M(
            "Click the box (or press Enter), then press the key combination.",
            "欄をクリック（または Enter）してから、キーの組み合わせを押してください。",
            "点击输入框（或按 Enter）后，按下组合键。",
            "點擊輸入框（或按 Enter）後，按下組合鍵。",
            "Haga clic en el cuadro (o pulse Intro) y luego pulse la combinación de teclas.",
            "Cliquez dans le champ (ou appuyez sur Entrée), puis sur la combinaison de touches.",
            "Feld anklicken (oder Eingabetaste drücken), dann die Tastenkombination drücken.",
            "Clique na caixa (ou pressione Enter) e depois pressione a combinação de teclas.",
            "Щёлкните поле (или нажмите Enter), затем нажмите сочетание клавиш.",
            "상자를 클릭(또는 Enter)한 다음 키 조합을 누르세요."),
        ["settings.hotkeyCapturing"] = M(
            "Press a key combination... (Esc: cancel, Backspace: clear)", "キー組み合わせを押してください... (Esc: キャンセル / Backspace: クリア)",
            "按下组合键...（Esc：取消，Backspace：清除）", "按下組合鍵...（Esc：取消，Backspace：清除）",
            "Pulse una combinación de teclas... (Esc: cancelar, Retroceso: borrar)", "Appuyez sur une combinaison de touches... (Échap : annuler, Retour arrière : effacer)",
            "Tastenkombination drücken... (Esc: abbrechen, Rücktaste: löschen)", "Pressione uma combinação de teclas... (Esc: cancelar, Backspace: limpar)",
            "Нажмите сочетание клавиш... (Esc — отмена, Backspace — очистить)", "키 조합을 누르세요... (Esc: 취소, Backspace: 지우기)"),
        ["settings.hotkeyNeedsModifier"] = M(
            "Add Ctrl, Alt or Win — or use F1–F24", "Ctrl・Alt・Win と組み合わせるか、F1～F24 を使ってください",
            "请加上 Ctrl、Alt 或 Win，或使用 F1–F24", "請加上 Ctrl、Alt 或 Win，或使用 F1–F24",
            "Añada Ctrl, Alt o Win, o use F1–F24", "Ajoutez Ctrl, Alt ou Win, ou utilisez F1–F24",
            "Strg, Alt oder Win hinzufügen – oder F1–F24 verwenden", "Adicione Ctrl, Alt ou Win, ou use F1–F24",
            "Добавьте Ctrl, Alt или Win либо используйте F1–F24", "Ctrl, Alt, Win 중 하나를 함께 누르거나 F1–F24를 사용하세요"),
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
            "Move up", "上へ移動",
            "上移", "上移",
            "Subir", "Monter",
            "Nach oben", "Mover para cima",
            "Вверх", "위로 이동"),
        ["settings.moveDown"] = M(
            "Move down", "下へ移動",
            "下移", "下移",
            "Bajar", "Descendre",
            "Nach unten", "Mover para baixo",
            "Вниз", "아래로 이동"),
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

        ["settings.switchCommunications"] = M(
            "Also switch the communications device (used for calls)",
            "通話用デバイス（Teams・Discord など）も切り替える",
            "同时切换通信设备（用于通话）",
            "同時切換通訊裝置（用於通話）",
            "Cambiar también el dispositivo de comunicaciones (llamadas)",
            "Changer aussi le périphérique de communication (appels)",
            "Auch das Kommunikationsgerät (für Anrufe) wechseln",
            "Alternar também o dispositivo de comunicação (chamadas)",
            "Переключать и устройство связи (для звонков)",
            "통신 장치(통화용)도 함께 전환"),
        ["settings.ok"] = Same("OK"),
        ["settings.cancel"] = M(
            "Cancel", "キャンセル",
            "取消", "取消",
            "Cancelar", "Annuler",
            "Abbrechen", "Cancelar",
            "Отмена", "취소"),
        ["settings.current"] = M(
            "(current)", "(現在)",
            "（当前）", "（目前）",
            "(actual)", "(actuel)",
            "(aktuell)", "(atual)",
            "(текущее)", "(현재)"),
        ["settings.devicesHint"] = M(
            "The hotkey switches to the next device in this list. Offline devices are skipped.",
            "ホットキーを押すたびに、この一覧の次のデバイスへ切り替わります。未接続のデバイスは飛ばします。",
            "每按一次热键就切换到列表中的下一个设备。未连接的设备会被跳过。",
            "每按一次快速鍵就切換到清單中的下一個裝置。未連接的裝置會被略過。",
            "La tecla rápida cambia al siguiente dispositivo de esta lista. Se omiten los desconectados.",
            "Le raccourci passe au périphérique suivant de cette liste. Les périphériques hors ligne sont ignorés.",
            "Die Tastenkombination wechselt zum nächsten Gerät dieser Liste. Nicht verbundene Geräte werden übersprungen.",
            "A tecla de atalho muda para o próximo dispositivo desta lista. Dispositivos desconectados são ignorados.",
            "Сочетание клавиш переключает на следующее устройство из списка. Отключённые устройства пропускаются.",
            "단축키를 누르면 이 목록의 다음 장치로 전환됩니다. 연결되지 않은 장치는 건너뜁니다."),
        ["settings.noNewDevices"] = M(
            "(no new devices available)", "(追加可能なデバイスがありません)",
            "（没有可添加的新设备）", "（沒有可新增的新裝置）",
            "(no hay dispositivos nuevos disponibles)", "(aucun nouveau périphérique disponible)",
            "(keine neuen Geräte verfügbar)", "(nenhum dispositivo novo disponível)",
            "(новых устройств нет)", "(추가 가능한 새 장치가 없습니다)"),

        // Modifier key names as printed on keyboards (German layouts label them "Strg" / "Umschalt").
        ["key.ctrl"] = M("Ctrl", "Ctrl", "Ctrl", "Ctrl", "Ctrl", "Ctrl", "Strg", "Ctrl", "Ctrl", "Ctrl"),
        ["key.alt"] = M("Alt", "Alt", "Alt", "Alt", "Alt", "Alt", "Alt", "Alt", "Alt", "Alt"),
        ["key.shift"] = M("Shift", "Shift", "Shift", "Shift", "Shift", "Shift", "Umschalt", "Shift", "Shift", "Shift"),
        ["key.win"] = M("Win", "Win", "Win", "Win", "Win", "Win", "Win", "Win", "Win", "Win"),

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
        ["error.hotkeyInvalid"] = M(
            "This key combination can't be used as a global hotkey.", "このキーの組み合わせはグローバルホットキーとして使用できません。",
            "此组合键无法用作全局热键。", "此組合鍵無法作為全域快速鍵使用。",
            "Esta combinación de teclas no se puede usar como tecla rápida global.", "Cette combinaison de touches ne peut pas être utilisée comme raccourci global.",
            "Diese Tastenkombination kann nicht als globales Tastenkürzel verwendet werden.", "Esta combinação de teclas não pode ser usada como tecla de atalho global.",
            "Это сочетание клавиш нельзя использовать как глобальное.", "이 키 조합은 전역 단축키로 사용할 수 없습니다."),
        ["error.hotkeysSame"] = M(
            "The two hotkeys must be different.",
            "2 つのホットキーには別々のキーを設定してください。",
            "两个热键必须不同。",
            "兩個快速鍵必須不同。",
            "Las dos teclas rápidas deben ser distintas.",
            "Les deux raccourcis doivent être différents.",
            "Die beiden Tastenkombinationen müssen sich unterscheiden.",
            "As duas teclas de atalho devem ser diferentes.",
            "Два сочетания клавиш должны различаться.",
            "두 단축키는 서로 달라야 합니다."),
        ["balloon.title"] = M(
            "Audio Carousel is running",
            "Audio Carousel は起動しています",
            "Audio Carousel 正在运行",
            "Audio Carousel 正在執行",
            "Audio Carousel se está ejecutando",
            "Audio Carousel est en cours d'exécution",
            "Audio Carousel läuft",
            "O Audio Carousel está em execução",
            "Audio Carousel запущен",
            "Audio Carousel이 실행 중입니다"),
        ["balloon.withHotkey"] = M(
            "Press {0} to switch to the next output. Right-click this icon for settings.",
            "{0} で次の出力へ切り替えます。設定はこのアイコンを右クリック。",
            "按 {0} 切换到下一个输出。右键点击此图标可打开设置。",
            "按 {0} 切換到下一個輸出。以滑鼠右鍵按一下此圖示可開啟設定。",
            "Pulse {0} para cambiar a la siguiente salida. Clic derecho en este icono para la configuración.",
            "Appuyez sur {0} pour passer à la sortie suivante. Clic droit sur cette icône pour les paramètres.",
            "Drücken Sie {0}, um zur nächsten Ausgabe zu wechseln. Rechtsklick auf dieses Symbol für Einstellungen.",
            "Pressione {0} para alternar para a próxima saída. Clique com o botão direito neste ícone para as configurações.",
            "Нажмите {0}, чтобы переключиться на следующий выход. Настройки — правый щелчок по этому значку.",
            "{0}을(를) 눌러 다음 출력으로 전환합니다. 설정은 이 아이콘을 마우스 오른쪽 버튼으로 클릭하세요."),
        ["balloon.noDevices"] = M(
            "Right-click this icon and open Settings to add the outputs you want to cycle through.",
            "このアイコンを右クリックして設定を開き、切り替えたい出力デバイスを追加してください。",
            "右键点击此图标并打开设置，添加要循环切换的输出设备。",
            "以滑鼠右鍵按一下此圖示並開啟設定，新增要循環切換的輸出裝置。",
            "Haga clic derecho en este icono y abra Configuración para agregar las salidas entre las que quiere alternar.",
            "Faites un clic droit sur cette icône et ouvrez les Paramètres pour ajouter les sorties à parcourir.",
            "Klicken Sie mit der rechten Maustaste auf dieses Symbol und öffnen Sie die Einstellungen, um die gewünschten Ausgabegeräte hinzuzufügen.",
            "Clique com o botão direito neste ícone e abra Configurações para adicionar as saídas entre as quais alternar.",
            "Щёлкните этот значок правой кнопкой и откройте настройки, чтобы добавить устройства вывода для переключения.",
            "이 아이콘을 마우스 오른쪽 버튼으로 클릭하고 설정을 열어 전환할 출력 장치를 추가하세요."),
        ["balloon.noHotkeyMenu"] = M(
            "Right-click this icon to switch outputs. Click it to open Settings.",
            "このアイコンを右クリックすると出力を切り替えられます。クリックで設定を開きます。",
            "右键点击此图标即可切换输出。单击可打开设置。",
            "以滑鼠右鍵按一下此圖示即可切換輸出。按一下可開啟設定。",
            "Haga clic derecho en este icono para cambiar de salida. Haga clic para abrir Configuración.",
            "Faites un clic droit sur cette icône pour changer de sortie. Cliquez pour ouvrir les Paramètres.",
            "Rechtsklick auf dieses Symbol wechselt die Ausgabe. Linksklick öffnet die Einstellungen.",
            "Clique com o botão direito neste ícone para alternar saídas. Clique para abrir Configurações.",
            "Щёлкните этот значок правой кнопкой, чтобы переключить выход. Щелчок открывает настройки.",
            "이 아이콘을 마우스 오른쪽 버튼으로 클릭해 출력을 전환하세요. 클릭하면 설정이 열립니다."),
        ["balloon.noHotkey"] = M(
            "Click this icon to switch to the next output. Right-click it for settings.",
            "このアイコンのクリックで次の出力へ切り替えます。設定は右クリック。",
            "点击此图标切换到下一个输出。右键点击可打开设置。",
            "按一下此圖示切換到下一個輸出。以滑鼠右鍵按一下可開啟設定。",
            "Haga clic en este icono para cambiar a la siguiente salida. Clic derecho para la configuración.",
            "Cliquez sur cette icône pour passer à la sortie suivante. Clic droit pour les paramètres.",
            "Klicken Sie auf dieses Symbol, um zur nächsten Ausgabe zu wechseln. Rechtsklick für Einstellungen.",
            "Clique neste ícone para alternar para a próxima saída. Clique com o botão direito para as configurações.",
            "Щёлкните этот значок, чтобы переключиться на следующий выход. Правый щелчок — настройки.",
            "이 아이콘을 클릭하면 다음 출력으로 전환됩니다. 설정은 마우스 오른쪽 버튼으로 클릭하세요."),
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
        ["error.noDevicesConfigured"] = M(
            "No devices to cycle yet — right-click the tray icon and open Settings to add some",
            "切替デバイスが未登録です — トレイアイコンを右クリックして設定から追加してください",
            "尚未添加要切换的设备 — 请右键点击托盘图标，在设置中添加",
            "尚未新增要切換的裝置 — 請以滑鼠右鍵按一下系統匣圖示，在設定中新增",
            "Aún no hay dispositivos en el ciclo: haga clic derecho en el icono de la bandeja y abra Configuración",
            "Aucun périphérique à parcourir : faites un clic droit sur l'icône et ouvrez Paramètres",
            "Noch keine Geräte im Wechsel – Rechtsklick auf das Taskleistensymbol und Einstellungen öffnen",
            "Nenhum dispositivo no ciclo ainda — clique com o botão direito no ícone da bandeja e abra Configurações",
            "Устройства для переключения не добавлены — щёлкните значок в трее правой кнопкой и откройте настройки",
            "순환할 장치가 없습니다 — 트레이 아이콘을 마우스 오른쪽 버튼으로 클릭해 설정에서 추가하세요"),
        ["error.noInputDevicesConfigured"] = M(
            "No microphones to cycle yet — add them on the Recording tab in Settings",
            "切替用のマイクが未登録です — 設定の「録音」タブで追加してください",
            "尚未添加要切换的麦克风 — 请在设置的“录制”选项卡中添加",
            "尚未新增要切換的麥克風 — 請在設定的「錄製」索引標籤中新增",
            "Aún no hay micrófonos en el ciclo: agréguelos en la pestaña Grabación de Configuración",
            "Aucun microphone à parcourir : ajoutez-en dans l'onglet Enregistrement des Paramètres",
            "Noch keine Mikrofone im Wechsel – im Tab „Aufnahme“ der Einstellungen hinzufügen",
            "Nenhum microfone no ciclo ainda — adicione-os na guia Gravação das Configurações",
            "Микрофоны для переключения не добавлены — добавьте их на вкладке «Запись» в настройках",
            "순환할 마이크가 없습니다 — 설정의 '녹음' 탭에서 추가하세요"),
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
        ["error.configUnreadable"] = M(
            "audio-carousel.json could not be read (another program may be using it). Audio Carousel is running with default settings and will not save changes until it is restarted.",
            "audio-carousel.json を読み込めませんでした（別のプログラムが使用中の可能性があります）。既定の設定で動作し、再起動するまで変更は保存しません。",
            "无法读取 audio-carousel.json（可能正被其他程序使用）。Audio Carousel 正以默认设置运行，重新启动前不会保存更改。",
            "無法讀取 audio-carousel.json（可能正由其他程式使用）。Audio Carousel 正以預設設定執行，重新啟動前不會儲存變更。",
            "No se pudo leer audio-carousel.json (puede que otro programa lo esté usando). Audio Carousel funciona con la configuración predeterminada y no guardará cambios hasta que se reinicie.",
            "Impossible de lire audio-carousel.json (un autre programme l'utilise peut-être). Audio Carousel fonctionne avec les paramètres par défaut et n'enregistrera aucune modification avant son redémarrage.",
            "audio-carousel.json konnte nicht gelesen werden (möglicherweise wird die Datei von einem anderen Programm verwendet). Audio Carousel läuft mit Standardeinstellungen und speichert bis zum Neustart keine Änderungen.",
            "Não foi possível ler audio-carousel.json (outro programa pode estar usando o arquivo). O Audio Carousel está usando as configurações padrão e não salvará alterações até ser reiniciado.",
            "Не удалось прочитать audio-carousel.json (возможно, файл занят другой программой). Audio Carousel работает с настройками по умолчанию и не будет сохранять изменения до перезапуска.",
            "audio-carousel.json을 읽을 수 없습니다(다른 프로그램이 사용 중일 수 있음). Audio Carousel은 기본 설정으로 실행되며 다시 시작할 때까지 변경 내용을 저장하지 않습니다."),
        ["error.startupFailed"] = M(
            "Couldn't change the \"Start with Windows\" setting. Windows may be blocking changes to startup apps.",
            "「Windows起動時に開始」を変更できませんでした。Windows がスタートアップアプリの変更をブロックしている可能性があります。",
            "无法更改“随 Windows 启动”设置。Windows 可能阻止了对启动应用的更改。",
            "無法變更「隨 Windows 啟動」設定。Windows 可能封鎖了啟動應用程式的變更。",
            "No se pudo cambiar «Iniciar con Windows». Puede que Windows esté bloqueando los cambios en las aplicaciones de inicio.",
            "Impossible de modifier « Démarrer avec Windows ». Windows bloque peut-être les modifications des applications de démarrage.",
            "„Mit Windows starten“ konnte nicht geändert werden. Möglicherweise blockiert Windows Änderungen an Autostart-Apps.",
            "Não foi possível alterar \"Iniciar com o Windows\". O Windows pode estar bloqueando alterações nos aplicativos de inicialização.",
            "Не удалось изменить параметр «Запускать с Windows». Возможно, Windows блокирует изменения автозагрузки.",
            "'Windows 시작 시 실행' 설정을 변경할 수 없습니다. Windows가 시작 앱 변경을 차단하고 있을 수 있습니다."),
        ["error.saveFailed"] = M(
            "Failed to save settings. Make sure the folder containing the app is writable.",
            "設定の保存に失敗しました。アプリのあるフォルダーが書き込み可能か確認してください。",
            "保存设置失败。请确认应用所在文件夹可写。",
            "儲存設定失敗。請確認應用程式所在資料夾可寫入。",
            "Error al guardar la configuración. Asegúrese de que la carpeta que contiene la aplicación sea escribible.",
            "Échec de l'enregistrement des paramètres. Vérifiez que le dossier contenant l'application est accessible en écriture.",
            "Speichern der Einstellungen fehlgeschlagen. Stellen Sie sicher, dass der Ordner mit der Anwendung beschreibbar ist.",
            "Falha ao salvar as configurações. Verifique se a pasta que contém o aplicativo permite gravação.",
            "Не удалось сохранить настройки. Убедитесь, что папка с приложением доступна для записи.",
            "설정을 저장하지 못했습니다. 앱이 있는 폴더에 쓰기가 가능한지 확인하세요."),
        ["error.unhandled"] = M(
            "An unexpected error occurred:", "予期しないエラーが発生しました:",
            "发生意外错误:", "發生未預期的錯誤:",
            "Se produjo un error inesperado:", "Une erreur inattendue s'est produite :",
            "Ein unerwarteter Fehler ist aufgetreten:", "Ocorreu um erro inesperado:",
            "Произошла непредвиденная ошибка:", "예기치 못한 오류가 발생했습니다:"),

        ["about.body"] = M(
            "Audio Carousel — switch the default audio output device with a global hotkey.",
            "Audio Carousel — グローバルホットキーで音声出力デバイスを切り替えます。",
            "Audio Carousel — 通过全局热键切换默认音频输出设备。",
            "Audio Carousel — 透過全域快速鍵切換預設音訊輸出裝置。",
            "Audio Carousel — cambia el dispositivo de salida de audio predeterminado con una tecla rápida global.",
            "Audio Carousel — change le périphérique de sortie audio par défaut avec un raccourci global.",
            "Audio Carousel — wechselt mit einer globalen Tastenkombination das Standard-Audiowiedergabegerät.",
            "Audio Carousel — alterna o dispositivo de saída de áudio padrão com uma tecla de atalho global.",
            "Audio Carousel — переключает аудиоустройство по умолчанию глобальным сочетанием клавиш.",
            "Audio Carousel — 전역 단축키로 기본 오디오 출력 장치를 전환합니다."),
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
