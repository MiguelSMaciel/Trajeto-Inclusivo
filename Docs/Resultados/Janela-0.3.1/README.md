# Evidências de janela e enquadramento

Versão 0.3.1, fontes da entrega no commit `d38a4d27eb1d0c16f8cfefbc65312eca3476d7cb`.

- `window-editmode.xml`: 62 testes aprovados, zero falhas, incluindo o layout em oito proporções e tamanhos.
- `window-playmode.xml`: 16 testes aprovados, zero falhas; projeção dos rótulos conferida com o viewport recortado.
- `window-navigation.xml`: reexecução do teste de navegação após ajustar o bloqueio de seleção nas margens externas; aprovada.
- `Windows/Stage4-Player-Matrix.csv`: 60/60 combinações aprovadas.
- `Windows/Stage4-Player.txt`: quatro mudanças de tamanho da janela com viewport, rótulos, margem superior e experiência corretos. Também confere os fluxos de inspeção nas três cenas e os seis fatores de velocidade; zero erros de execução.
- `Windows/WindowLayout-*.png`: capturas completas com IMGUI, conferidas em 1600×1000, 1280×720, 1920×1080 e 1024×768.
- `Windows/window-chrome.json`: leitura dos estilos nativos da janela. `WS_THICKFRAME` e `WS_MAXIMIZEBOX` estão habilitados; não houve automação de clique físico no botão Maximizar.
- `Windows/SHA256SUMS-Windows.txt`: checksum do ZIP publicado, baixado novamente e comparado com o arquivo local. O executável e a biblioteca do simulador dentro do pacote coincidem com os utilizados na validação.
- `Linux/Stage4-Player-Matrix.csv` e `Linux/Stage4-Player.txt`: 60/60 combinações, quatro tamanhos reais da janela, viewport, rótulos e margens corretos, experiência preservada e zero erros de execução.
- `Linux/environment.txt` e `Linux/package-sha256.txt`: Ubuntu 22.04.5 e checksum do pacote publicado, conferido pelo workflow junto com o manifesto de runtime.
- `Linux/WindowLayout-*.png`: capturas completas da interface; 1920×1080 e 1024×768 conferidas visualmente.

Linux: [workflow 40745406737](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40745406737), concluído com sucesso sobre os fontes da entrega, usando Xvfb/Mesa. O ambiente não possui gerenciador de janelas de desktop para verificar seu botão nativo de maximizar. Os registros Linux foram acrescentados ao repositório após a publicação, sem substituir os pacotes testados.

Os testes do player alteram o tamanho com a API do aplicativo. A rotina confirma que o tamanho efetivamente mudou e verifica o framebuffer renderizado. O ensaio não representa um teste de usabilidade com participantes.

As imagens geográficas utilizam referências OpenStreetMap, com calçadas e conexões aproximadas. **© OpenStreetMap contributors — ODbL 1.0.**
