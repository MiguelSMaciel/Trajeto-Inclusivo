# Evidências de desempenho e áudio

Versão 0.3.2, fontes da entrega no commit `82ae2e05172bbd92816853e057b0949e1046f763`.

- `playmode.xml`: 16 testes de integração reexecutados e aprovados, zero falhas. O último relatório das 62 verificações EditMode está em [Janela-0.3.1](../Janela-0.3.1/window-editmode.xml).
- `configuration.json`: conferência da configuração do projeto com áudio Unity desativado, volume zero, módulo de áudio ausente do manifest e teto solicitado de 60 FPS com VSync 0.
- `Windows/Stage4-Player-Matrix.csv`: 60/60 combinações aprovadas no executável.
- `Windows/Stage4-Player.txt`: teto de 60 FPS confirmado nas três cenas. Amostras de 30 quadros por tempo não escalado ficaram em 60,0, 59,5 e 60,0 FPS, dentro da tolerância de temporização de 20%. O relatório também confirma inspeção, seis velocidades de reprodução, quatro tamanhos da janela e zero erros de execução.
- `Windows/window-chrome.json`: redimensionamento e maximização continuam habilitados.
- `Windows/SHA256SUMS-Windows.txt`: checksum do ZIP publicado, baixado novamente e comparado com o pacote local. O executável e a biblioteca do simulador dentro do ZIP coincidem com os utilizados na validação.
- `Linux/Stage4-Player-Matrix.csv` e `Linux/Stage4-Player.txt`: 60/60 combinações, teto solicitado de 60 FPS com VSync 0 nas três cenas, inspeção, velocidades e quatro tamanhos de janela aprovados; zero erros de execução.
- `Linux/environment.txt` e `Linux/package-sha256.txt`: ambiente e checksum do pacote publicado. O workflow também conferiu o manifesto dos componentes do player.

Linux: [workflow 40748263203](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40748263203), concluído com sucesso em Ubuntu 22.04.5 com Xvfb/Mesa. A renderização por software produziu amostras de 2,8, 3,1 e 5,1 FPS; a configuração de teto foi confirmada, sem comprovação de renderização a 60 FPS nesse ambiente. Os registros Linux foram acrescentados ao repositório após a validação, sem substituir os pacotes testados.

A amostra de FPS verifica o limite de renderização, sem exigir uma taxa mínima em máquinas lentas. Não é um benchmark de GPU ou CPU. Não houve medição acústica, de consumo elétrico ou diagnóstico da origem física do som relatado. A melhora do ruído do gabinete precisa ser conferida no computador em que ele ocorre.
