class CreateUserExpressions < ActiveRecord::Migration[5.0]
  def change
    create_table :user_expressions do |t|
      t.integer :expression
      t.binary :face_img
      t.references :user, foreign_key: true

      t.timestamps
    end
  end
end
