import os
import numpy as np
import tensorflow as tf
from tensorflow.keras.preprocessing import image
from flask import Flask, jsonify, request, render_template
from tensorflow.keras.preprocessing.image import ImageDataGenerator

app = Flask(__name__, template_folder='.')

TRAINING_DIR = 'C:/Users/gusta/Desktop/TrabalhoHiran/IA/train'
MODEL_DIRECTORY = 'C:/Users/gusta/Desktop/TrabalhoHiran/IA'
UPLOAD_FOLDER = 'C:/Users/gusta/Desktop/TrabalhoHiran/IA/uploads'

# Altere o diretório de trabalho para o diretório do modelo
os.chdir(MODEL_DIRECTORY)

# Compile o modelo pronto
model = tf.keras.models.load_model('best_model.h5')

batch_size = 1  #define o tamanho do lote
target_size = 200 #tamanho da imagem

train_datagen = ImageDataGenerator(rescale=1./255)
train_generator = train_datagen.flow_from_directory(
    TRAINING_DIR, 
    target_size=(target_size,target_size),
    class_mode='categorical',
    batch_size=batch_size,
    color_mode='rgb',
)

# Obter o mapeamento de classes para índices do gerador de treinamento
classe_indices = train_generator.class_indices

indices_to_classes = {v: k for k, v in classe_indices.items()}

app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER
@app.route('/', methods=['GET', 'POST'])
def upload_file():
    classification = None
    if request.method == 'POST':
        file = request.files['file']
        if file:
            filename = os.path.join(app.config['UPLOAD_FOLDER'], file.filename)
            file.save(filename)

            
            # Opção 2: Inserir o caminho ou a imagem para fazer previsão
            user_input = filename

            try:
                # Tente carregar a imagem do caminho fornecido ou da entrada do usuário
                img = image.load_img(user_input, target_size=(target_size, target_size))
                img_array = image.img_to_array(img)
                img_array = np.expand_dims(img_array, axis=0)

                # Pré-processamento da imagem
                img_array = img_array / 255.0  # Normalização

                # Faça a previsão usando o modelo
                predictions = model.predict(img_array)

                # Obtenha a classe prevista
                predicted_label = indices_to_classes[np.argmax(predictions)]

                # Exiba a classe prevista
                classification = predicted_label
              
            except Exception as e:
                return f'\nErro ao processar a imagem: {str(e)}'

    return jsonify({'classification': classification}) 


if __name__ == '__main__':
    app.run()
