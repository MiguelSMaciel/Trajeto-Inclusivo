# Evidências da inspeção de nós

Versão 0.3.0, fontes da entrega no commit `fe34796f4c8d5b87388b78e73d293f0e06df590a`.

- `editmode.xml`: 59 testes aprovados, zero falhas.
- `playmode.xml`: 16 testes aprovados, zero falhas, incluindo três testes novos de inspeção.
- `Windows/Stage4-Player-Matrix.csv`: 60/60 combinações aprovadas no executável Windows.
- `Windows/Stage4-Player.txt`: seleção por raycast, consulta sem editar o mundo, localização de alterações, uso como A/B e fechamento nas três cenas; seis fatores de reprodução; zero erros de execução.
- `Windows/Nodes-*.png`: capturas completas com IMGUI, conferidas em 1600×1000 e 1024×768.
- `Windows/SHA256SUMS-Windows.txt`: checksum do ZIP publicado, baixado novamente e comparado com o arquivo local. O executável e a biblioteca do simulador dentro do ZIP coincidem com os utilizados na validação.
- `Linux/Stage4-Player-Matrix.csv` e `Linux/Stage4-Player.txt`: 60/60 combinações, inspeção nas três cenas, seis fatores de reprodução e zero erros de execução.
- `Linux/environment.txt` e `Linux/package-sha256.txt`: ambiente Ubuntu 22.04.5 e checksum do pacote validado com Xvfb/Mesa. O workflow também conferiu o manifesto interno dos componentes do player.
- `Linux/Nodes-*.png`: capturas da interface. A inspeção de GeoRamp e da referência da FAESA foi conferida visualmente.

Execução Linux: [workflow 40740969228](https://github.com/MiguelSMaciel/Trajeto-Inclusivo/actions/runs/40740969228), concluído com sucesso sobre os fontes da entrega. As evidências Linux e este índice foram acrescentados ao repositório após a publicação, preservando os pacotes testados.

Os testes exercitam as ações públicas e raycasts usados pela interface, sem automação de cliques físicos na janela. A alteração visual final ampliou o anel de seleção para aparecer ao redor dos marcadores maiores; o executável foi validado novamente depois desse ajuste.

A inspeção continua mostrando condições do modelo. Não houve levantamento de campo ou ensaio de usabilidade com participantes. As imagens geográficas usam referências do OpenStreetMap e calçadas aproximadas. **© OpenStreetMap contributors — ODbL 1.0.**
